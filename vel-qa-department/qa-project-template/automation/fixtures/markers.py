"""Shared pytest markers."""
import os

import pytest

BUILD = os.getenv("BUILD", "1.0")


def known(defect: str, builds=("1.0",)):
    """Mark a KNOWN (seeded) defect as an expected failure on the builds where it exists.

    strict=True: when developers fix the defect the test passes -> the run FAILS until this marker is removed
    and the defect is closed. The suite stays green but honest; never use xfail to hide an unexplained failure.
    """
    return pytest.mark.xfail(condition=BUILD in builds, strict=True, reason=f"Known defect {defect} (builds {'/'.join(builds)})")
