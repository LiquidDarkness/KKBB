using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class LottoTester
{
    private enum TestEnum
    {
        none,
        neutral,
        buff,
        debuff
    }

    [MenuItem("My/Test Lotto")]
    public static void Test()
    {
        var dictEasy = new Dictionary<TestEnum, int>()
        {
            { TestEnum.none, 5 },
            { TestEnum.buff, 3 },
            { TestEnum.neutral, 1 },
            { TestEnum.debuff, 1 }
        };
        Lotto<TestEnum> testEasyLotto = new Lotto<TestEnum>();
        testEasyLotto.FillBucket(dictEasy);

        var dictNormal = new Dictionary<TestEnum, int>()
        {
            { TestEnum.none, 5 },
            { TestEnum.buff, 2 },
            { TestEnum.neutral, 2 },
            { TestEnum.debuff, 1 }
        };
        Lotto<TestEnum> testNormalLotto = new Lotto<TestEnum>();
        testNormalLotto.FillBucket(dictNormal);

        var dictHard = new Dictionary<TestEnum, int>()
        {
            { TestEnum.none, 5 },
            { TestEnum.buff, 1 },
            { TestEnum.neutral, 2 },
            { TestEnum.debuff, 2 }
        };
        Lotto<TestEnum> testHardLotto = new Lotto<TestEnum>();
        testHardLotto.FillBucket(dictHard);

        for (int i = 0; i < 10; i++)
        {
            Debug.Log(testEasyLotto.GetRandomTicket());
        }
    }
}
