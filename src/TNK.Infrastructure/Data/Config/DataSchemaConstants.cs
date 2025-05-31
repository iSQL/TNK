namespace TNK.Infrastructure.Data.Config;

/// <summary>
/// Contains constants used for defining database schema properties,
/// such as default string lengths, schema names, etc.
/// </summary>
public static class DataSchemaConstants
{
  /// <summary>
  /// Default schema name for tables (e.g., "dbo" for SQL Server).
  /// Adjust if you are using a different default schema.
  /// </summary>
  public const string DefaultSchema = "dbo";

  /// <summary>
  /// Default maximum length for name-like properties (e.g., Service.Name, Worker.FirstName).
  /// </summary>
  public const int DEFAULT_NAME_LENGTH = 255; // Increased from 100 for more flexibility

  /// <summary>
  /// Default maximum length for description-like properties.
  /// </summary>
  public const int DEFAULT_DESCRIPTION_LENGTH = 1000;

  /// <summary>
  /// Default maximum length for URL strings.
  /// </summary>
  public const int DEFAULT_URL_LENGTH = 2048;

  /// <summary>
  /// Default maximum length for email address strings.
  /// (RFC 5321 specifies mailbox (local-part@domain) max length of 254, but 320 is sometimes used for safety)
  /// </summary>
  public const int DEFAULT_EMAIL_LENGTH = 320;


  /// <summary>
  /// Default maximum length for phone number strings.
  /// Accommodates international numbers and formatting.
  /// </summary>
  public const int DEFAULT_PHONE_LENGTH = 50;

  /// <summary>
  /// Maximum length for short codes or identifiers.
  /// </summary>
  public const int SHORT_CODE_LENGTH = 50;

  /// <summary>
  /// Maximum length for general-purpose text fields that might hold more than a simple description.
  /// </summary>
  public const int EXTENDED_TEXT_LENGTH = 4000; // Max for nvarchar(4000) in SQL Server before nvarchar(max)

  // You can add other constants here as needed, for example:
  // public const int ZIP_CODE_LENGTH = 10;
  // public const int CURRENCY_CODE_LENGTH = 3;
  // public const string DEFAULT_DECIMAL_PRECISION = "decimal(18, 2)"; // For use in HasColumnType if preferred over separate precision/scale
}
