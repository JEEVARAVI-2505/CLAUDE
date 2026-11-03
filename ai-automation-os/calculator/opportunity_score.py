"""Weighted opportunity scoring (Phase 1.4). Scores are 1-5 where 5 = most favourable
(complexity, error risk and competition are already inverted). They are the author's
HYPOTHESES to be replaced by customer-interview evidence at Gate 1."""
W = dict(severity=15, frequency=10, time_savings=15, financial=10, feasibility=15,
         ai_need=5, integration=5, error_risk=5, demand=15, competition=5)
assert sum(W.values()) == 100
C = {
 "GST purchase-bill processing (CA firms/SMEs)": (4,5,5,4,4,4,3,3,4,3),
 "Real-estate WhatsApp lead qualification":      (3,5,3,4,4,3,4,4,4,2),
 "Customer-support RAG assistant":               (3,5,3,3,4,5,4,3,4,2),
 "Recruitment CV screening":                     (3,3,4,3,4,4,4,3,3,2),
 "Construction tender/BOQ extraction":           (4,3,5,4,3,4,3,3,3,4),
 "Clinic appointment + follow-up WhatsApp":      (3,5,3,3,5,2,3,4,4,3),
 "E-commerce returns / RTO handling":            (4,5,4,4,3,3,2,3,4,2),
 "Legal admin document drafting":                (3,3,3,3,3,4,4,2,3,3),
 "Logistics POD / e-way bill documents":         (4,4,4,4,3,4,2,3,3,4),
 "School fee reminders + enquiries":             (2,4,3,2,5,2,4,4,3,3),
}
if __name__ == "__main__":
    rows = sorted(((sum(w*s for w, s in zip(W.values(), v))/5, k) for k, v in C.items()), reverse=True)
    for sc, k in rows: print(f"{sc:5.1f}  {k}")
