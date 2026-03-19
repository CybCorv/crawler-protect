using System.Text;

namespace CrawlerProtect;

/// <summary>
/// Generates a self-contained JavaScript decoder tailored to the current
/// <see cref="CrawlerProtectOptions"/>. Each deployment gets its own unique script,
/// so a bot cannot reuse a decoder written for a different site.
/// </summary>
/// <remarks>
/// Serve the result via <c>app.MapCrawlerProtectDecoder()</c> and include it with:
/// <code>&lt;script src="/crawler-protect.js"&gt;&lt;/script&gt;</code>
/// </remarks>
public static class CrawlerProtectJsGenerator
{
    /// <summary>
    /// Generates a minified, self-invoking JavaScript decoder with the format
    /// parameters baked in as literals (not named constants).
    /// </summary>
    public static string Generate(CrawlerProtectOptions options)
    {
        var sep  = JsEscape(options.Separator.ToString());
        var attr = JsEscape(options.DataAttribute);

        var sb = new StringBuilder();

        sb.AppendLine("(function(){");

        // ── _d(e) : decode one encoded value ──────────────────────────────
        sb.Append("function _d(e){");

        if (options.KeyPosition == KeyPosition.Before)
            sb.Append($"var si=e.indexOf(\"{sep}\"),key=parseInt(e.substring(0,si),10),data=e.substring(si+1);");
        else
            sb.Append($"var si=e.lastIndexOf(\"{sep}\"),data=e.substring(0,si),key=parseInt(e.substring(si+1),10);");

        if (options.PayloadEncoding == PayloadEncoding.Base64)
            sb.Append("var bin=atob(data),b=new Uint8Array(bin.length);for(var i=0;i<bin.length;i++)b[i]=bin.charCodeAt(i)^key;");
        else
            sb.Append("var b=new Uint8Array(data.length/2);for(var i=0;i<data.length;i+=2)b[i/2]=parseInt(data.substring(i,i+2),16)^key;");

        sb.AppendLine("return new TextDecoder().decode(b);}");

        // ── _l(c) : decode protected email links ───────────────────────────
        sb.Append("function _l(c){");
        sb.Append("var a=Array.from(document.getElementsByClassName(c));");
        sb.Append("for(var i=0;i<a.length;i++){");
        sb.Append(   $"var e=a[i],v=e.getAttribute(\"{attr}\");");
        sb.Append(    "if(!v)continue;");
        sb.Append(    "var t=_d(v);");
        sb.Append(    "e.textContent=t;");
        sb.Append(   $"e.removeAttribute(\"{attr}\");");
        sb.Append(    "e.href=\"mailto:\"+t;");
        sb.AppendLine("}}");

        // ── _s(c) : decode protected text spans ────────────────────────────
        sb.Append("function _s(c){");
        sb.Append("var a=Array.from(document.getElementsByClassName(c));");
        sb.Append("for(var i=a.length-1;i>=0;i--){");
        sb.Append(   $"var e=a[i],v=e.getAttribute(\"{attr}\");");
        sb.Append(    "if(!v)continue;");
        sb.Append(    "var s=document.createElement(\"span\");");
        sb.Append(    "s.textContent=_d(v);");
        sb.Append(    "for(var j=0;j<e.attributes.length;j++){");
        sb.Append(       "var at=e.attributes[j];");
        sb.Append(      $"if(at.name!==\"{attr}\")s.setAttribute(at.name,at.value);");
        sb.Append(    "}");
        sb.Append(    "e.parentElement&&e.parentElement.replaceChild(s,e);");
        sb.AppendLine("}}");

        sb.AppendLine("_l(\"protected-lnk\");_s(\"protected-str\");");
        sb.Append("})();");

        return sb.ToString();
    }

    // Escapes a string for safe embedding as a JavaScript double-quoted string literal.
    private static string JsEscape(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("\"", "\\\"")
         .Replace("\n",  "\\n")
         .Replace("\r",  "\\r");
}
