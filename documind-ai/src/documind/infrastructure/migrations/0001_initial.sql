-- DocuMind AI schema v1
CREATE TABLE IF NOT EXISTS schema_migrations(version INTEGER PRIMARY KEY, name TEXT NOT NULL, applied_at TEXT NOT NULL);

CREATE TABLE documents(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  file_name TEXT NOT NULL,
  sha256 TEXT NOT NULL,
  stored_path TEXT NOT NULL,
  mime TEXT NOT NULL,
  size_bytes INTEGER NOT NULL,
  pages INTEGER NOT NULL DEFAULT 0,
  status TEXT NOT NULL CHECK(status IN ('queued','processing','needs_review','approved','rejected','failed')),
  kind TEXT NOT NULL DEFAULT 'unknown',
  imported_at TEXT NOT NULL,
  processed_at TEXT,
  reviewed_at TEXT,
  approved_at TEXT,
  error TEXT,
  supplier TEXT,
  doc_number TEXT,
  doc_date TEXT,
  total TEXT,
  currency TEXT,
  issue_errors INTEGER NOT NULL DEFAULT 0,
  issue_warnings INTEGER NOT NULL DEFAULT 0,
  info_json TEXT NOT NULL DEFAULT '{}'
);
CREATE INDEX ix_documents_sha ON documents(sha256);
CREATE INDEX ix_documents_status ON documents(status);
CREATE INDEX ix_documents_doc_date ON documents(doc_date);
CREATE INDEX ix_documents_imported ON documents(imported_at);
CREATE INDEX ix_documents_supplier ON documents(supplier COLLATE NOCASE);

CREATE TABLE field_values(
  document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
  row_index INTEGER NOT NULL DEFAULT -1,
  name TEXT NOT NULL,
  value TEXT,
  extracted_value TEXT,
  raw TEXT,
  page INTEGER,
  source TEXT NOT NULL,
  origin TEXT NOT NULL CHECK(origin IN ('extracted','corrected')),
  confirmed INTEGER NOT NULL DEFAULT 0,
  flags TEXT NOT NULL DEFAULT '[]',
  alt TEXT,
  PRIMARY KEY(document_id, row_index, name)
);

CREATE TABLE validation_issues(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
  code TEXT NOT NULL,
  severity TEXT NOT NULL CHECK(severity IN ('info','warning','error')),
  message TEXT NOT NULL,
  field TEXT,
  row_index INTEGER,
  expected TEXT,
  actual TEXT
);
CREATE INDEX ix_issues_doc ON validation_issues(document_id);

CREATE TABLE audit_log(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  document_id INTEGER NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
  ts TEXT NOT NULL,
  actor TEXT NOT NULL,
  action TEXT NOT NULL,
  field TEXT,
  row_index INTEGER,
  old_value TEXT,
  new_value TEXT,
  note TEXT
);
CREATE INDEX ix_audit_doc ON audit_log(document_id, ts);

-- Content-free record that a permanent deletion happened (no business data).
CREATE TABLE deletion_log(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  deleted_at TEXT NOT NULL,
  document_id INTEGER NOT NULL,
  reason TEXT NOT NULL,
  actor TEXT NOT NULL
);
