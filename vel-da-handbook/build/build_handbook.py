"""Build the VEL Data Analytics & BI Handbook PDF.  Run: python3 build_handbook.py"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))

from reportlab.lib.units import mm
from reportlab.platypus import Image, PageBreak, Paragraph, Spacer
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.lib.styles import ParagraphStyle

import content_a as A
import content_b as B
import content_c as C
from vel_pdf import DOC_ID, DOC_VER, H1, LOGO, NAVY, S, Doc, NumberedCanvas, P

OUT = Path(__file__).parent.parent / "output" / "VEL-Data-Analytics-BI-Handbook.pdf"


def cover():
    img = Image(LOGO, width=110 * mm, height=110 * mm * 630 / 936)
    img.hAlign = "CENTER"
    return [Spacer(1, 18 * mm), img, Spacer(1, 16 * mm),
            Paragraph("Data Analytics &amp; Business Intelligence<br/>Department Handbook", S["ctitle"]),
            Spacer(1, 10 * mm),
            Paragraph("Training, Project Engineering and Client Delivery Operating System<br/>"
                      "Beginner to Professional · 0 to 100% Project Lifecycle", S["csub"]),
            Spacer(1, 18 * mm),
            Paragraph("Developed by <b><font color='#1B2F6B'>V. LOKESHWARAN</font></b><br/>VEL Software Solutions<br/>"
                      "velsoftwaresolutions.in@gmail.com", S["cmid"]),
            Spacer(1, 10 * mm),
            Paragraph(f"Document {DOC_ID} · Version {DOC_VER} · Module 1: Foundation, Onboarding and 90-Day Plan",
                      S["small"].clone("c", alignment=1)),
            PageBreak()]


def toc():
    t = TableOfContents()
    t.levelStyles = [ParagraphStyle("t0", fontName="Helvetica", fontSize=10, leading=17, leftIndent=8)]
    return H1("Contents") + [t, Spacer(1, 8),
                             P("All prices and market figures in this handbook's examples are placeholders until verified "
                               "from official sources (Part 9 and Part 11.3).", "small")]


def main():
    OUT.parent.mkdir(exist_ok=True)
    doc = Doc(str(OUT))
    story = cover() + toc() + [PageBreak()]
    for part in (A.part0, A.part1, A.part2, A.part3, B.part4, B.part5, B.part6, B.part7,
                 C.part8, C.part9, C.part10, C.part11, C.part12, C.part13, C.annexes):
        story += part()
    doc.multiBuild(story, canvasmaker=NumberedCanvas)
    print("Built", OUT)


if __name__ == "__main__":
    main()
