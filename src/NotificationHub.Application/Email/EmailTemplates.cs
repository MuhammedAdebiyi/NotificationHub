using System.Net;

namespace NotificationHub.Application.Email;

/// <summary>
/// Shared branded email layout. Every system email goes through Wrap() so
/// they all share the same header, typography, colors, and footer.
/// Brand palette matches client/src/index.css.
/// </summary>
public static class EmailTemplates
{
    // Brand palette — client/src/index.css
    public const string Ink = "#15131B";
    public const string Violet = "#6D28D9";
    public const string Teal = "#0E9F84";
    public const string Coral = "#FF6452";
    public const string Fog = "#F6F5F2";
    public const string MutedText = "#6B6875";

    private const string FontStack =
        "-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,'Helvetica Neue',Arial,sans-serif";

    public static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    public static string Button(string url, string label, string color = Violet) => $"""
        <table role="presentation" cellpadding="0" cellspacing="0" style="margin:24px 0;">
          <tr>
            <td align="center" bgcolor="{color}" style="border-radius:8px;">
              <a href="{Encode(url)}" style="display:inline-block;padding:14px 28px;font-family:{FontStack};font-size:15px;font-weight:600;color:#FFFFFF;text-decoration:none;border-radius:8px;">{Encode(label)}</a>
            </td>
          </tr>
        </table>
        """;

    public static string Wrap(
        string preheader,
        string innerHtml,
        string? footerNote = null)
    {
        var note = footerNote
            ?? "You're receiving this email because an account exists at notificationhub.space.";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <meta name="color-scheme" content="light">
              <meta name="supported-color-schemes" content="light">
              <title>{Encode(preheader)}</title>
            </head>
            <body style="margin:0;padding:0;background-color:{Fog};font-family:{FontStack};-webkit-font-smoothing:antialiased;">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{Encode(preheader)}&#847;&zwnj;&nbsp;&#847;&zwnj;&nbsp;&#847;&zwnj;&nbsp;</div>
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:{Fog};margin:0;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="width:100%;max-width:600px;background-color:#FFFFFF;border-radius:16px;overflow:hidden;border:1px solid rgba(21,19,27,0.08);">
                      <tr>
                        <td style="padding:24px 32px;border-bottom:1px solid rgba(21,19,27,0.08);">
                          <table role="presentation" cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="vertical-align:middle;padding-right:10px;">
                                <div style="width:26px;height:26px;background-color:{Violet};border-radius:7px;text-align:center;line-height:26px;font-size:15px;font-weight:700;color:#FFFFFF;">N</div>
                              </td>
                              <td style="vertical-align:middle;font-family:{FontStack};font-size:17px;font-weight:700;color:{Ink};letter-spacing:-0.3px;">
                                Notification<span style="color:{Violet};">Hub</span>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:32px;">
                          {innerHtml}
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:20px 32px 28px;border-top:1px solid rgba(21,19,27,0.08);">
                          <p style="margin:0 0 6px;font-size:12px;line-height:1.6;color:{MutedText};">{Encode(note)}</p>
                          <p style="margin:0;font-size:12px;line-height:1.6;color:{MutedText};">
                            NotificationHub &middot; <a href="https://notificationhub.space" style="color:{Violet};text-decoration:none;">notificationhub.space</a>
                          </p>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    public static string H1(string text) =>
        $"<h1 style=\"margin:0 0 16px;font-family:{FontStack};font-size:24px;font-weight:700;color:{Ink};letter-spacing:-0.5px;line-height:1.25;\">{Encode(text)}</h1>";

    public static string P(string html) =>
        $"<p style=\"margin:0 0 16px;font-family:{FontStack};font-size:15px;line-height:1.65;color:{Ink};\">{html}</p>";

    public static string Muted(string html) =>
        $"<p style=\"margin:16px 0 0;font-family:{FontStack};font-size:13px;line-height:1.6;color:{MutedText};\">{html}</p>";
}
