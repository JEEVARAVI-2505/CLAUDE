"""Structured error types.

Every error carries a stable ``code`` (for logs/tests) and a ``user_message``
that is safe and understandable to show a customer. Technical detail goes in
``detail`` and must never contain document contents.
"""

from __future__ import annotations


class DocuMindError(Exception):
    code = "E_GENERIC"

    def __init__(self, user_message: str, *, detail: str | None = None) -> None:
        super().__init__(user_message)
        self.user_message = user_message
        self.detail = detail

    def __str__(self) -> str:  # pragma: no cover - trivial
        return self.user_message


# --- file validation -------------------------------------------------------
class FileValidationError(DocuMindError):
    code = "E_FILE"


class UnsupportedFileTypeError(FileValidationError):
    code = "E_FILE_TYPE"


class FileTooLargeError(FileValidationError):
    code = "E_FILE_SIZE"


class EmptyFileError(FileValidationError):
    code = "E_FILE_EMPTY"


class CorruptedFileError(FileValidationError):
    code = "E_FILE_CORRUPT"


class PasswordProtectedError(FileValidationError):
    code = "E_FILE_PASSWORD"


class TooManyPagesError(FileValidationError):
    code = "E_FILE_PAGES"


class FileAccessError(FileValidationError):
    code = "E_FILE_ACCESS"


# --- OCR -------------------------------------------------------------------
class OcrError(DocuMindError):
    code = "E_OCR"


class OcrUnavailableError(OcrError):
    code = "E_OCR_MISSING"


class OcrLanguageMissingError(OcrError):
    code = "E_OCR_LANG"


# --- AI --------------------------------------------------------------------
class AiError(DocuMindError):
    code = "E_AI"


class AiUnavailableError(AiError):
    code = "E_AI_DOWN"


class AiModelMissingError(AiError):
    code = "E_AI_MODEL"


class AiTimeoutError(AiError):
    code = "E_AI_TIMEOUT"


class AiMalformedOutputError(AiError):
    code = "E_AI_OUTPUT"


class AiRemoteHostBlockedError(AiError):
    code = "E_AI_REMOTE"


# --- persistence / ops -----------------------------------------------------
class DatabaseError(DocuMindError):
    code = "E_DB"


class DatabaseLockedError(DatabaseError):
    code = "E_DB_LOCKED"


class MigrationError(DatabaseError):
    code = "E_DB_MIGRATION"


class IntegrityCheckError(DatabaseError):
    code = "E_DB_INTEGRITY"


class StorageError(DocuMindError):
    code = "E_STORAGE"


class DiskFullError(StorageError):
    code = "E_DISK_FULL"


class PermissionDeniedError(StorageError):
    code = "E_PERMISSION"


class ExportError(DocuMindError):
    code = "E_EXPORT"


class BackupError(DocuMindError):
    code = "E_BACKUP"


class RestoreValidationError(BackupError):
    code = "E_RESTORE_INVALID"


class ConfigError(DocuMindError):
    code = "E_CONFIG"


class OperationCancelled(DocuMindError):
    code = "E_CANCELLED"

    def __init__(self, user_message: str = "The operation was cancelled.") -> None:
        super().__init__(user_message)


class NotFoundError(DocuMindError):
    code = "E_NOT_FOUND"


class InvalidValueError(DocuMindError):
    code = "E_VALUE"


class ApprovalBlockedError(DocuMindError):
    code = "E_APPROVAL"
