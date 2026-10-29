import os
import shutil
from pathlib import Path

import pytest
from PIL import Image

from documind.domain.exceptions import (CorruptedFileError, EmptyFileError, FileAccessError, FileTooLargeError, PasswordProtectedError,
                                        TooManyPagesError, UnsupportedFileTypeError)
from documind.config import LimitSettings
from documind.infrastructure.file_storage import preflight, safe_filename, sniff_mime
from documind.infrastructure.pdf_reader import open_source
from documind.samples_pdf import write_text_pdf
from tests.helpers import make_ctx, make_encrypted_pdf, make_png, sample_pdf

L = LimitSettings()


def test_valid_pdf_text_and_pages(tmp_path):
    doc, p = sample_pdf(tmp_path, 1, "gst_classic")
    ext, mime, size = preflight(p, L)
    assert (ext, mime) == (".pdf", "application/pdf") and size > 0
    src = open_source(p, mime, L)
    assert src.page_count == 1 and doc.number in src.embedded_text(0)
    assert src.image(0, 72).width > 100
    src.close()


def test_multipage_pdf(tmp_path):
    lines = [f"line {i}" for i in range(150)]
    from documind.samples_pdf import paginate
    p = tmp_path / "m.pdf"; write_text_pdf(paginate(lines), p)
    src = open_source(p, "application/pdf", L)
    assert src.page_count == 3 and "line 0" in src.embedded_text(0) and "line 149" in src.embedded_text(2)


def test_empty_file_rejected(tmp_path):
    p = tmp_path / "e.pdf"; p.write_bytes(b"")
    with pytest.raises(EmptyFileError):
        preflight(p, L)


def test_corrupted_pdf_rejected(tmp_path):
    _, good = sample_pdf(tmp_path, 1)
    bad = tmp_path / "bad.pdf"; bad.write_bytes(good.read_bytes()[:150])  # truncated: header ok, body gone
    preflight(bad, L)
    with pytest.raises(CorruptedFileError):
        open_source(bad, "application/pdf", L)
    junk = tmp_path / "junk.pdf"; junk.write_bytes(b"%PDF-1.4\n" + os.urandom(2000))
    with pytest.raises(CorruptedFileError):
        open_source(junk, "application/pdf", L)


def test_password_protected_pdf_rejected_clearly(tmp_path):
    p = make_encrypted_pdf(tmp_path / "enc.pdf")
    with pytest.raises(PasswordProtectedError) as e:
        open_source(p, "application/pdf", L)
    assert "password" in e.value.user_message.lower()


def test_extension_is_not_trusted(tmp_path):
    png = make_png(tmp_path / "a.png")
    spoof = tmp_path / "spoof.pdf"; shutil.copy(png, spoof)
    with pytest.raises(UnsupportedFileTypeError) as e:
        preflight(spoof, L)
    assert "contents" in e.value.user_message
    exe = tmp_path / "evil.pdf"; exe.write_bytes(b"MZ\x90\x00" + b"\x00" * 100)
    with pytest.raises(UnsupportedFileTypeError):
        preflight(exe, L)
    txt = tmp_path / "n.txt"; txt.write_text("hi")
    with pytest.raises(UnsupportedFileTypeError):
        preflight(txt, L)


def test_large_file_rejected(tmp_path):
    p = tmp_path / "big.pdf"; p.write_bytes(b"%PDF-1.4\n" + b"0" * (2 * 1024 * 1024))
    with pytest.raises(FileTooLargeError):
        preflight(p, LimitSettings(max_file_mb=1))


def test_page_limit(tmp_path):
    from documind.samples_pdf import paginate
    p = tmp_path / "m.pdf"; write_text_pdf(paginate([f"l{i}" for i in range(200)]), p)
    with pytest.raises(TooManyPagesError):
        open_source(p, "application/pdf", LimitSettings(max_pages=2))


@pytest.mark.parametrize("ext,fmt", [(".png", "PNG"), (".jpg", "JPEG"), (".jpeg", "JPEG"), (".tif", "TIFF"), (".tiff", "TIFF")])
def test_image_formats_accepted(tmp_path, ext, fmt):
    img = Image.new("RGB", (300, 200), "white"); p = tmp_path / f"i{ext}"; img.save(p, fmt)
    e, mime, _ = preflight(p, L)
    src = open_source(p, mime, L)
    assert src.page_count == 1 and src.image(0).size == (300, 200)


def test_multipage_tiff(tmp_path):
    frames = [Image.new("L", (200, 100), c) for c in (255, 200, 150)]
    p = tmp_path / "m.tiff"; frames[0].save(p, "TIFF", save_all=True, append_images=frames[1:])
    assert open_source(p, "image/tiff", L).page_count == 3


def test_truncated_and_bomb_images_rejected(tmp_path):
    ok = tmp_path / "ok.png"; Image.new("RGB", (400, 400), "white").save(ok)
    cut = tmp_path / "cut.png"; cut.write_bytes(ok.read_bytes()[:200])
    with pytest.raises(CorruptedFileError):
        open_source(cut, "image/png", L)
    big = tmp_path / "big.png"; Image.new("L", (9000, 9000), 255).save(big)  # 81 MP
    with pytest.raises(CorruptedFileError):
        open_source(big, "image/png", LimitSettings(max_image_megapixels=50))


def test_missing_and_directory_paths(tmp_path):
    with pytest.raises(FileAccessError):
        preflight(tmp_path / "nope.pdf", L)
    with pytest.raises(UnsupportedFileTypeError):
        preflight(tmp_path, L)


def test_unreadable_file_permission(tmp_path):
    p = tmp_path / "x.pdf"; sample_pdf(tmp_path, 1, name="x.pdf"); os.chmod(p, 0)
    if os.access(p, os.R_OK):
        pytest.skip("running as root: permissions not enforced")
    with pytest.raises(FileAccessError):
        preflight(p, L)


@pytest.mark.parametrize("name", ["../../etc/passwd", "..\\..\\windows\\system32\\x.pdf", "CON", "a<b>c|d?.pdf", "x\x00y.pdf", "  ..  ", "nul.pdf",
                                  "日本語の請求書.pdf", "a" * 400 + ".pdf"])
def test_malicious_filenames_neutralised(name):
    s = safe_filename(name)
    assert "/" not in s and "\\" not in s and "\x00" not in s and ".." not in s.split(".")[0] and len(s) <= 112
    assert s.split(".")[0].lower() not in {"con", "nul"}


def test_unicode_path_import_and_duplicate_detection(tmp_home):
    ctx = make_ctx()
    folder = tmp_home / "ग्राहक दस्तावेज़"; folder.mkdir()
    doc, p = sample_pdf(folder, 3, name="बिल №1.pdf")
    r1 = ctx.docs.import_one(p)
    assert r1.status == "imported"
    r2 = ctx.docs.import_one(p)
    assert r2.status == "duplicate" and r2.existing_ids == [r1.doc_id]
    copy = folder / "copy.pdf"; shutil.copy(p, copy)
    assert ctx.docs.import_one(copy).status == "duplicate"           # same bytes, different name
    assert ctx.docs.import_one(copy, skip_duplicates=False).status == "imported"
    assert ctx.repo.get(r1.doc_id).file_name == "बिल №1.pdf"
    ctx.shutdown()


def test_import_batch_limit_and_isolated_failures(tmp_home):
    ctx = make_ctx(); ctx.cfg.limits.max_batch_files = 3
    good = [sample_pdf(tmp_home, s, name=f"g{s}.pdf")[1] for s in (1, 2)]
    bad = tmp_home / "bad.pdf"; bad.write_bytes(b"%PDF-1.4 junk")
    extra = [sample_pdf(tmp_home, 9, name="g9.pdf")[1]]
    res = ctx.docs.import_files(good + [bad] + extra)
    assert [r.status for r in res] == ["imported", "imported", "rejected", "rejected"]
    assert "limit" in res[3].message.lower()
    ctx.shutdown()
