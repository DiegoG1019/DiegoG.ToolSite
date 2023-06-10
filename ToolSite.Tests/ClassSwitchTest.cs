using System.Diagnostics;
using DiegoG.ToolSite.Client.Types;

namespace ToolSite.Tests;

[TestClass]
public class ClassSwitchTest
{
    private readonly ClassSwitch Switch = new("a-0", "b-1", "c-2", "d-3", "e-4", "f-5", "g-6");

    [TestMethod]
    public void Creation()
    {
        ClassSwitch sw;
        sw = new("a", "b-x", "c", "d", "e", "f", "g", "h");

        bool failed = false;
        try
        {
            sw = new("aaa", "a-bc-d", "a v");
        }
        catch
        {
            failed = true;
        }

        Debug.Assert(failed, "Switch did not fail CSS class validation of invalid CSS class names");
    }

    [TestMethod]
    public void SingleUsage()
    {
        Debug.Assert(Switch.Class == "", "Switch class is not empty");

        Switch.Enable(3);

        Debug.Assert(Switch.Class == "d-3", $"Switch class is not equal to 'd-3', but to '{Switch.Class}'");
    }

    [TestMethod]
    public void MultipleUsage()
    {
        Debug.Assert(Switch.Class == "", "Switch class is not empty");

        for (int i = 0; i < Switch.Count; i++)
            Switch.Enable(i);

        Debug.Assert(Switch.Class == "a-0 b-1 c-2 d-3 e-4 f-5 g-6", $"Switch class is not equal to 'a-0 b-1 c-2 d-3 e-4 f-5 g-6', but to '{Switch.Class}'");
    }

    [TestMethod]
    public void RandomUsage()
    {
        for (int iteration = 0; iteration < 20; iteration++)
        {
            int limit = Random.Shared.Next(1, Switch.Count);
            HashSet<int> used = new();
            int count;
            for (count = 0; count < limit; count++)
            {
                int x;

                do
                    x = Random.Shared.Next(0, Switch.Count - 1);
                while (used.Contains(x));

                used.Add(x);
                Switch.Enable(x);
            }

            Console.WriteLine("Using the following indices: {0}", string.Join(", ", used));
            Console.WriteLine("Obtained class: {0}", Switch.Class);
            Console.WriteLine("-----------------------------------------");

            int spaceCount = Switch.Class.Count(x => x == ' ') + 1;
            Debug.Assert(spaceCount == count, $"Switch class space count is different than expected. Was {spaceCount}, expected {count}");

            for (int i = 0; i < Switch.Count; i++)
                Switch.Disable(i);
        }
    }
}