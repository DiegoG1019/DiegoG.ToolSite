using System.Text;
using DiegoG.ToolSite.Shared;

namespace DiegoG.ToolSite.Client.Types;

public class ClassSwitch
{
    private readonly string[] classes;
    private readonly bool[] switches;
    private readonly StringBuilder sb;

    public ClassSwitch(params string[] classes)
    {
        this.classes = classes;
        switches = new bool[classes.Length];

        int len = 0;
        for (int i = 0; i < classes.Length; i++) 
        {
            var cl = classes[i];
            if (string.IsNullOrWhiteSpace(cl) || RegexHelpers.VerifyValidCssClassRegex().IsMatch(classes[i]) is false)
                throw new InvalidOperationException("CSS Classes must not contain spaces, non-numbers or non-letter characters except for '-' and '_'. They must also not start with a number");
            len += cl.Length + 1;
        }

        sb = new(len);
    }

    private string? _cls;
    public string Class
    {
        get
        {
            if (_cls is null)
            {
                sb.Clear();
                bool cutoff = false;
                for (int i = 0; i < switches.Length; i++)
                    if (switches[i])
                    {
                        sb.Append(classes[i]).Append(' ');
                        cutoff = true;
                    }

                if (cutoff)
                    sb.Remove(sb.Length - 1, 1);
                _cls = sb.ToString();
            }

            return _cls;
        }
    }

    public void Enable(int index)
        => this[index] = true;

    public void Disable(int index)
        => this[index] = false;

    public int Count => switches.Length;

    public bool this[int index]
    {
        get => switches[index];
        set
        {
            if (switches[index] != value)
            {
                _cls = null;
                switches[index] = value;
            }
        }
    }
}
