"""Full pipeline: render HTML, 2-pass PDF with TOC page numbers, per-template PDFs."""
import json, re, subprocess, sys
from pathlib import Path
HERE = Path(__file__).resolve().parent; ROOT = HERE.parent; TMP = HERE / "_tmp"; OUT = ROOT / "output"
sys.path.insert(0, str(HERE))
import render

HL = render.HEADER_L; FL = "VEL Software Solutions · Custom Software &amp; Enterprise Solutions Department"

def pdf(inp, out, hl=HL, fl=FL):
    subprocess.run(["node", str(HERE / "pdf.js"), str(inp), str(out), hl, fl], check=True, cwd=HERE)

def page_texts(pdfpath):
    n = int(re.search(r"Pages:\s+(\d+)", subprocess.run(["pdfinfo", str(pdfpath)], capture_output=True, text=True).stdout).group(1))
    return [re.sub(r"\s+", " ", subprocess.run(["pdftotext", "-f", str(i), "-l", str(i), str(pdfpath), "-"], capture_output=True, text=True).stdout) for i in range(1, n + 1)]

def norm(s): return re.sub(r"\s+", " ", s).strip()

def main():
    subprocess.run([sys.executable, str(HERE / "render.py")], check=True)
    meta = json.loads((TMP / "meta.json").read_text())
    tmp_pdf = TMP / "pass1.pdf"; final = OUT / "VEL-Custom-Software-Enterprise-Handbook.pdf"
    pdf(TMP / "print.html", tmp_pdf)
    for attempt in range(2):
        texts = page_texts(tmp_pdf)
        start = next(i for i, t in enumerate(texts) if "This handbook is the single operating manual" in t)
        pages = {}
        for ch in meta["chapters"]:
            key = norm(ch["label"])
            hit = next((i for i in range(start, len(texts)) if key in texts[i]), None)
            if hit is None: print("NOT FOUND:", key)
            else: pages[ch["id"]] = hit + 1
            for hid, ht in ch["h3"]:
                k = norm(ht); h = next((i for i in range(start, len(texts)) if k in texts[i]), None)
                if h is not None: pages[hid] = h + 1
                else: print("NOT FOUND:", k)
        (TMP / "pages.json").write_text(json.dumps(pages))
        subprocess.run([sys.executable, str(HERE / "render.py"), "--pages"], check=True)
        pdf(TMP / "print.html", final)
        tmp_pdf = final
    print("handbook pages:", len(page_texts(final)))
    (OUT / "templates").mkdir(exist_ok=True)
    for ch in meta["chapters"]:
        if ch["kind"] == "appendix":
            letter = ch["id"].split("-")[1].upper()
            name = re.sub(r"[^A-Za-z0-9]+", "-", ch["label"].split("—", 1)[1].strip()).strip("-")
            pdf(TMP / f"{ch['id']}.html", OUT / "templates" / f"VEL-Appendix-{letter}-{name}.pdf",
                hl=f"VEL Software Solutions · {norm(ch['label'])}", fl=FL)
    print("done")

if __name__ == "__main__":
    main()
