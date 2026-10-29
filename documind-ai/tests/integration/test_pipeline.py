import pytest

from documind.domain.models import DocStatus
from documind.application.extraction_service import CancelToken
from documind.domain.exceptions import OperationCancelled
from documind.infrastructure.ocr_engine import TesseractEngine
from documind.samples import generate, write_sample
from tests.helpers import make_ctx, make_png, process_all, sample_pdf, tesseract_available

needs_tess = pytest.mark.skipif(not tesseract_available(), reason="Tesseract not installed")


def test_digital_pdf_end_to_end(tmp_home):
    ctx = make_ctx()
    d, p = sample_pdf(tmp_home, 21, "gst_classic")
    r = ctx.docs.import_one(p)
    rec = process_all(ctx, [r.doc_id])[0]
    assert rec.status == DocStatus.NEEDS_REVIEW and rec.info.text_method == "embedded" and rec.info.mode == "rules"
    assert rec.get("invoice_number") == d.number and rec.get("total") == "%s" % d.truth["total"]
    assert len(rec.items) == len(d.items) and rec.error_count == 0
    assert all(f.origin == "extracted" and not f.confirmed for f in rec.fields.values())
    assert rec.fields["invoice_number"].page == 1 and rec.fields["invoice_number"].raw
    ctx.shutdown()


def test_inconsistent_document_is_flagged_not_fixed(tmp_home):
    ctx = make_ctx()
    d = generate(22, "gst_classic")
    d.lines = [("                                                  Grand Total        Rs. 99,999.00" if "Grand Total" in l else l) for l in d.lines]
    p = tmp_home / "bad_total.pdf"; write_sample(d, p)
    rec = process_all(ctx, [ctx.docs.import_one(p).doc_id])[0]
    bad = [i for i in rec.issues if i.code == "TOTAL_MISMATCH"]
    assert bad and bad[0].actual == "99999"
    assert rec.get("total") == "99999"                  # the printed value is kept as-is, never silently "corrected"
    ctx.shutdown()


@needs_tess
def test_scanned_png_and_image_pdf_via_real_ocr(tmp_home):
    ctx = make_ctx()
    d = generate(23, "gst_classic")
    png, ipdf = tmp_home / "scan.png", tmp_home / "scan.pdf"
    write_sample(d, png, as_scan=True); write_sample(d, ipdf, as_scan=True)
    recs = process_all(ctx, [ctx.docs.import_one(png).doc_id, ctx.docs.import_one(ipdf).doc_id])
    for rec in recs:
        assert rec.status == DocStatus.NEEDS_REVIEW and rec.info.text_method == "ocr" and rec.info.ocr_pages == [1]
        assert rec.info.ocr_score and rec.info.ocr_score > 60
        assert rec.get("invoice_number") == d.number
    ctx.shutdown()


@needs_tess
def test_rotated_scan_is_straightened(tmp_home):
    ctx = make_ctx(); d = generate(24, "gst_classic"); p = tmp_home / "rot.png"
    write_sample(d, p, as_scan=True, rotate=90)
    rec = process_all(ctx, [ctx.docs.import_one(p).doc_id])[0]
    assert rec.get("invoice_number") == d.number and any("rotated" in w for w in rec.info.warnings)
    ctx.shutdown()


def test_scan_without_ocr_fails_with_actionable_message(tmp_home, monkeypatch):
    monkeypatch.setattr(TesseractEngine, "locate", lambda self: None)
    ctx = make_ctx(); p = make_png(tmp_home / "s.png", ["Invoice No: 1"] * 3)
    rec = process_all(ctx, [ctx.docs.import_one(p).doc_id])[0]
    assert rec.status == DocStatus.FAILED and "OCR" in rec.error and "Install" in rec.error
    ctx.shutdown()


def test_missing_ocr_language_is_diagnosed(tmp_home):
    ctx = make_ctx(); ctx.cfg.ocr.languages = "xyz"; ctx.update_config(ctx.cfg, save=False)
    st = ctx.ocr.status(refresh=True)
    if st.version:
        assert not st.available and "xyz" in st.message
    ctx.shutdown()


def test_one_failed_file_does_not_affect_others(tmp_home):
    ctx = make_ctx()
    ids = [ctx.docs.import_one(sample_pdf(tmp_home, s, name=f"f{s}.pdf")[1]).doc_id for s in (31, 32, 33)]
    victim = ctx.repo.get(ids[1])
    open(victim.stored_path, "wb").write(b"%PDF-1.4 now damaged")          # corrupt after import
    recs = process_all(ctx, ids)
    assert [r.status for r in recs] == [DocStatus.NEEDS_REVIEW, DocStatus.FAILED, DocStatus.NEEDS_REVIEW]
    assert recs[1].error and ctx.repo.stats(None)["failed"] == 1
    assert ctx.db.integrity_check() == []
    ctx.shutdown()


def test_cancel_requeues_and_retry_works(tmp_home):
    ctx = make_ctx(); r = ctx.docs.import_one(sample_pdf(tmp_home, 41)[1])
    tok = CancelToken(); tok.cancel()
    with pytest.raises(OperationCancelled):
        ctx.pipeline.process(r.doc_id, tok)
    assert ctx.repo.get(r.doc_id).status == DocStatus.QUEUED
    assert process_all(ctx, [r.doc_id])[0].status == DocStatus.NEEDS_REVIEW
    ctx.shutdown()


def test_pause_resume_and_cancel_all(tmp_home):
    ctx = make_ctx()
    ids = [ctx.docs.import_one(sample_pdf(tmp_home, s, name=f"p{s}.pdf")[1]).doc_id for s in (51, 52, 53)]
    ctx.batch.pause(); ctx.batch.enqueue(ids)
    assert ctx.batch.paused and not ctx.batch.wait_idle(0.5)
    assert all(ctx.repo.get(i).status == DocStatus.QUEUED for i in ids)
    ctx.batch.resume(); assert ctx.batch.wait_idle(60)
    assert all(ctx.repo.get(i).status == DocStatus.NEEDS_REVIEW for i in ids)
    ids2 = [ctx.docs.import_one(sample_pdf(tmp_home, s, name=f"q{s}.pdf")[1]).doc_id for s in (54, 55)]
    ctx.batch.pause(); ctx.batch.enqueue(ids2); ctx.batch.cancel_all(); assert ctx.batch.wait_idle(30)
    assert all(ctx.repo.get(i).status == DocStatus.QUEUED for i in ids2)
    ctx.shutdown()


def test_business_duplicate_flagged(tmp_home):
    ctx = make_ctx(); d, p1 = sample_pdf(tmp_home, 61, name="a.pdf")
    p2 = tmp_home / "b.pdf"; write_sample(d, p2); p2.write_bytes(p2.read_bytes() + b"\n% different bytes, same invoice\n")
    a, b = process_all(ctx, [ctx.docs.import_one(p1).doc_id, ctx.docs.import_one(p2).doc_id])
    assert "DUPLICATE_BUSINESS" in {i.code for i in b.issues}
    ctx.shutdown()


def test_interrupted_processing_is_requeued_on_restart(tmp_home):
    ctx = make_ctx(); r = ctx.docs.import_one(sample_pdf(tmp_home, 71)[1])
    ctx.repo.set_status(r.doc_id, DocStatus.PROCESSING); ctx.shutdown()
    ctx2 = make_ctx()
    assert ctx2.repo.get(r.doc_id).status == DocStatus.QUEUED and any("interrupted" in n for n in ctx2.notices)
    ctx2.shutdown()
