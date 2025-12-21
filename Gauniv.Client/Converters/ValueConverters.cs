using System.Globalization;

namespace Gauniv.Client.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string texts)
            {
                var parts = texts.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Colors.Gray : Colors.Green;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    public class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<Models.CategoryDto> categories)
            {
                return string.Join(", ", categories.Select(c => c.Name));
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class StripHtmlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                
                var result = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
                // Décoder les entités HTML
                result = System.Net.WebUtility.HtmlDecode(result);
                return result;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class IsHtmlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return text.Contains("<html>", StringComparison.OrdinalIgnoreCase) || 
                       text.Contains("<p>", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("<div>", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("<br>", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("<strong>", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("<b>", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class HtmlToWebViewSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && !string.IsNullOrEmpty(text))
            {
                // Si ce n'est pas du HTMLon le wrappe dans un paragraphe simple
                var html = text;
                if (!text.Contains("<html>", StringComparison.OrdinalIgnoreCase) && 
                    !text.Contains("<p>", StringComparison.OrdinalIgnoreCase) &&
                    !text.Contains("<div>", StringComparison.OrdinalIgnoreCase) &&
                    !text.Contains("<h1>", StringComparison.OrdinalIgnoreCase) &&
                    !text.Contains("<h2>", StringComparison.OrdinalIgnoreCase) &&
                    !text.Contains("<h3>", StringComparison.OrdinalIgnoreCase))
                {
                    html = "<p>" + text.Replace("\n", "<br>") + "</p>";
                }
                
                // Wrapper HTML pourrendu avec style Steam
                var wrappedHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            background-color: #1B2838;
            color: #C7D5E0;
            padding: 15px;
            margin: 0;
            line-height: 1.6;
        }}
        h1, h2, h3 {{ color: #66C0F4; }}
        h1 {{ font-size: 24px; margin: 10px 0; }}
        h2 {{ font-size: 20px; margin: 8px 0; }}
        h3 {{ font-size: 18px; margin: 6px 0; }}
        p {{ margin: 10px 0; font-size: 16px; }}
        strong, b {{ color: #FFFFFF; font-weight: bold; }}
        em, i {{ color: #8F98A0; font-style: italic; }}
        ul, ol {{ margin: 10px 0; padding-left: 25px; }}
        li {{ margin: 5px 0; }}
        a {{ color: #66C0F4; text-decoration: none; }}
        a:hover {{ color: #5C9F3F; text-decoration: underline; }}
        .highlight {{ background-color: #FFA500; color: #000; padding: 2px 5px; }}
        .warning {{ color: #FFA500; font-weight: bold; }}
        .success {{ color: #5C9F3F; font-weight: bold; }}
        .error {{ color: #C23030; font-weight: bold; }}
    </style>
</head>
<body>
{html}
</body>
</html>";
                return new HtmlWebViewSource { Html = wrappedHtml };
            }
            return new HtmlWebViewSource { Html = "<p>No description available</p>" };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
