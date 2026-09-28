using System.Collections;
using CardGame.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CardGame.Tests
{
    public sealed class BattlePresenterSmokeTests
    {
        [UnityTest]
        public IEnumerator Presenter_CreatesRuntimeUiAndStartsBattle()
        {
            GameObject host = new GameObject("BattlePresenterSmokeTest");
            host.AddComponent<BattlePresenter>();

            yield return null;

            Transform canvas = host.transform.Find("BattleCanvas");
            Assert.IsNotNull(canvas, "BattlePresenter 应创建战斗 Canvas。");
            Assert.IsNotNull(canvas.Find("Hand"), "战斗 UI 应包含手牌区域。");
            Assert.IsNotNull(canvas.Find("EndTurnButton"), "战斗 UI 应包含结束回合按钮。");
            Assert.GreaterOrEqual(canvas.GetComponentsInChildren<Transform>().Length, 10);

            Object.Destroy(host);
            yield return null;
        }
    }
}
