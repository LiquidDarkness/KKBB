using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

// Checks the achievement register against itself. None of this touches the save, PlayerPrefs or
// Steam - it only reads what is written in the source - so it is safe to run at any time and says
// nothing about whether the game plays correctly.
//
// What it is for: the register is edited by hand every time a scenario or an achievement is added,
// and every mistake it can catch is one that would otherwise show up as an achievement quietly
// never unlocking on a player's machine.
public class AchievementRegisterTests
{
    private static List<string> Constants()
    {
        return typeof(Achievements)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue())
            .ToList();
    }

    private static Dictionary<string, bool> Register()
    {
        FieldInfo field = typeof(Achievements)
            .GetField("Register", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, "Achievements.Register is gone or was renamed.");

        return (Dictionary<string, bool>)field.GetValue(null);
    }

    [Test]
    public void EveryConstantIsInTheRegister()
    {
        Dictionary<string, bool> register = Register();

        foreach (string apiName in Constants())
        {
            Assert.That(register.ContainsKey(apiName), Is.True,
                $"'{apiName}' has a constant but no line in the register, so it can never be unlocked.");
        }
    }

    [Test]
    public void EveryRegisteredNameHasAConstant()
    {
        List<string> constants = Constants();

        foreach (string apiName in Register().Keys)
        {
            Assert.That(constants.Contains(apiName), Is.True,
                $"'{apiName}' is registered but no constant names it.");
        }
    }

    // The save file is one line per setting, split on '/', and the earned names are joined with a
    // comma. A name carrying either would corrupt every setting written after it.
    [Test]
    public void NoNameCarriesASeparator()
    {
        foreach (string apiName in Register().Keys)
        {
            Assert.That(apiName, Does.Not.Contain("/"), $"'{apiName}' would break the save file.");
            Assert.That(apiName, Does.Not.Contain(","), $"'{apiName}' would break the earned list.");
            Assert.That(string.IsNullOrWhiteSpace(apiName), Is.False, "an empty api name is registered.");
        }
    }

    // Steam matches on the api name, so two achievements sharing one would be the same achievement.
    [Test]
    public void NamesAreUnique()
    {
        List<string> constants = Constants();

        Assert.That(constants.Distinct().Count(), Is.EqualTo(constants.Count),
            "two constants hold the same api name.");
    }

    // Every scenario the tracker knows how to finish has to have somewhere to put the result.
    [Test]
    public void EveryScenarioAchievementIsRegistered()
    {
        FieldInfo field = typeof(AchievementTracker)
            .GetField("ScenarioAchievements", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(field, Is.Not.Null, "AchievementTracker.ScenarioAchievements is gone or was renamed.");

        var scenarios = (Dictionary<string, string>)field.GetValue(null);
        Dictionary<string, bool> register = Register();

        foreach (KeyValuePair<string, string> pair in scenarios)
        {
            Assert.That(register.ContainsKey(pair.Value), Is.True,
                $"scenario '{pair.Key}' points at '{pair.Value}', which is not registered.");
        }
    }
}
