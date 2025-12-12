using System.Text.RegularExpressions;

namespace MarFin_Final.Helpers
{
    public static class PhoneNumberValidator
    {
        // Dictionary of country codes and their phone number patterns
        private static readonly Dictionary<string, PhonePattern> CountryPatterns = new()
        {
            // Philippines
            { "+63", new PhonePattern
                {
                    CountryCode = "+63",
                    Pattern = @"^\+63\s?\d{3}\s?\d{3}\s?\d{4}$|^\+63\s?\(\d{3}\)\s?\d{3}\s?\d{4}$",
                    Example = "+63 912 345 6789 or +63 (912) 345 6789",
                    MinLength = 13,
                    MaxLength = 18
                }
            },
            
            // United States / Canada
            { "+1", new PhonePattern
                {
                    CountryCode = "+1",
                    Pattern = @"^\+1\s?\d{3}\s?\d{3}\s?\d{4}$|^\+1\s?\(\d{3}\)\s?\d{3}\s?\d{4}$",
                    Example = "+1 555 123 4567 or +1 (555) 123 4567",
                    MinLength = 12,
                    MaxLength = 17
                }
            },
            
            // United Kingdom
            { "+44", new PhonePattern
                {
                    CountryCode = "+44",
                    Pattern = @"^\+44\s?\d{4}\s?\d{6}$|^\+44\s?\d{3}\s?\d{3}\s?\d{4}$",
                    Example = "+44 20 1234 5678 or +44 7911 123456",
                    MinLength = 13,
                    MaxLength = 16
                }
            },
            
            // Australia
            { "+61", new PhonePattern
                {
                    CountryCode = "+61",
                    Pattern = @"^\+61\s?\d{1}\s?\d{4}\s?\d{4}$|^\+61\s?\d{3}\s?\d{3}\s?\d{3}$",
                    Example = "+61 4 1234 5678",
                    MinLength = 12,
                    MaxLength = 15
                }
            },
            
            // Japan
            { "+81", new PhonePattern
                {
                    CountryCode = "+81",
                    Pattern = @"^\+81\s?\d{1,4}\s?\d{1,4}\s?\d{4}$",
                    Example = "+81 90 1234 5678",
                    MinLength = 12,
                    MaxLength = 15
                }
            },
            
            // China
            { "+86", new PhonePattern
                {
                    CountryCode = "+86",
                    Pattern = @"^\+86\s?\d{3}\s?\d{4}\s?\d{4}$",
                    Example = "+86 138 0000 0000",
                    MinLength = 13,
                    MaxLength = 16
                }
            },
            
            // Singapore
            { "+65", new PhonePattern
                {
                    CountryCode = "+65",
                    Pattern = @"^\+65\s?\d{4}\s?\d{4}$",
                    Example = "+65 9123 4567",
                    MinLength = 11,
                    MaxLength = 13
                }
            },
            
            // Malaysia
            { "+60", new PhonePattern
                {
                    CountryCode = "+60",
                    Pattern = @"^\+60\s?\d{1,2}\s?\d{3,4}\s?\d{4}$",
                    Example = "+60 12 345 6789",
                    MinLength = 11,
                    MaxLength = 15
                }
            },
            
            // India
            { "+91", new PhonePattern
                {
                    CountryCode = "+91",
                    Pattern = @"^\+91\s?\d{5}\s?\d{5}$|^\+91\s?\d{4}\s?\d{3}\s?\d{3}$",
                    Example = "+91 98765 43210",
                    MinLength = 13,
                    MaxLength = 15
                }
            },
            
            // Germany
            { "+49", new PhonePattern
                {
                    CountryCode = "+49",
                    Pattern = @"^\+49\s?\d{3}\s?\d{3,9}$",
                    Example = "+49 151 12345678",
                    MinLength = 11,
                    MaxLength = 16
                }
            },
            
            // France
            { "+33", new PhonePattern
                {
                    CountryCode = "+33",
                    Pattern = @"^\+33\s?\d{1}\s?\d{2}\s?\d{2}\s?\d{2}\s?\d{2}$",
                    Example = "+33 6 12 34 56 78",
                    MinLength = 12,
                    MaxLength = 16
                }
            },
            
            // South Korea
            { "+82", new PhonePattern
                {
                    CountryCode = "+82",
                    Pattern = @"^\+82\s?\d{1,2}\s?\d{3,4}\s?\d{4}$",
                    Example = "+82 10 1234 5678",
                    MinLength = 12,
                    MaxLength = 15
                }
            }
        };

        public class PhonePattern
        {
            public string CountryCode { get; set; } = string.Empty;
            public string Pattern { get; set; } = string.Empty;
            public string Example { get; set; } = string.Empty;
            public int MinLength { get; set; }
            public int MaxLength { get; set; }
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? DetectedCountryCode { get; set; }
        }

        /// <summary>
        /// Validates a phone number against international formats
        /// </summary>
        public static ValidationResult ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Phone number is required"
                };
            }

            // Remove common formatting characters for validation
            string cleanNumber = phoneNumber.Trim();

            // Check if phone number starts with +
            if (!cleanNumber.StartsWith("+"))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Phone number must start with country code (e.g., +63)"
                };
            }

            // Try to detect country code
            foreach (var pattern in CountryPatterns)
            {
                if (cleanNumber.StartsWith(pattern.Key))
                {
                    // Validate against the country's pattern
                    var regex = new Regex(pattern.Value.Pattern);
                    if (regex.IsMatch(cleanNumber))
                    {
                        return new ValidationResult
                        {
                            IsValid = true,
                            Message = "Valid phone number",
                            DetectedCountryCode = pattern.Key
                        };
                    }
                    else
                    {
                        return new ValidationResult
                        {
                            IsValid = false,
                            Message = $"Invalid format for {pattern.Key}. Example: {pattern.Value.Example}",
                            DetectedCountryCode = pattern.Key
                        };
                    }
                }
            }

            // If no country code matched, provide general feedback
            return new ValidationResult
            {
                IsValid = false,
                Message = "Unsupported country code. Supported codes: " +
                         string.Join(", ", CountryPatterns.Keys.Take(5)) + "..."
            };
        }

        /// <summary>
        /// Formats a phone number with proper spacing
        /// </summary>
        public static string FormatPhoneNumber(string phoneNumber, string countryCode)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            // Remove all spaces, dashes, and parentheses
            string digitsOnly = Regex.Replace(phoneNumber, @"[\s\-\(\)]", "");

            if (!CountryPatterns.ContainsKey(countryCode))
                return phoneNumber;

            // Apply country-specific formatting
            switch (countryCode)
            {
                case "+63": // Philippines: +63 912 345 6789
                    if (digitsOnly.Length >= 12)
                    {
                        return $"{digitsOnly.Substring(0, 3)} {digitsOnly.Substring(3, 3)} {digitsOnly.Substring(6, 3)} {digitsOnly.Substring(9)}";
                    }
                    break;

                case "+1": // US/Canada: +1 555 123 4567
                    if (digitsOnly.Length >= 11)
                    {
                        return $"{digitsOnly.Substring(0, 2)} {digitsOnly.Substring(2, 3)} {digitsOnly.Substring(5, 3)} {digitsOnly.Substring(8)}";
                    }
                    break;

                case "+65": // Singapore: +65 9123 4567
                    if (digitsOnly.Length >= 10)
                    {
                        return $"{digitsOnly.Substring(0, 3)} {digitsOnly.Substring(3, 4)} {digitsOnly.Substring(7)}";
                    }
                    break;
            }

            return phoneNumber;
        }

        /// <summary>
        /// Gets all supported country codes with examples
        /// </summary>
        public static Dictionary<string, PhonePattern> GetSupportedCountries()
        {
            return CountryPatterns;
        }

        /// <summary>
        /// Gets phone number example for a specific country code
        /// </summary>
        public static string GetExample(string countryCode)
        {
            return CountryPatterns.ContainsKey(countryCode)
                ? CountryPatterns[countryCode].Example
                : "No example available";
        }
    }
}