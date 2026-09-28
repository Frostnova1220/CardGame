using UnityEngine;

namespace CardGame.Presentation
{
    /// <summary>
    /// 无需修改场景即可启动框架。Play 后会自动创建一个持久化应用节点。
    /// 正式项目接入主菜单后，可以删除这个自动入口，改由 MainMenu 场景手动创建。
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (UnityEngine.Object.FindAnyObjectByType<BattleApplication>() != null)
            {
                return;
            }

            GameObject applicationObject = new GameObject("[CardGameApplication]");
            UnityEngine.Object.DontDestroyOnLoad(applicationObject);
            applicationObject.AddComponent<BattleApplication>();
        }
    }

    public sealed class BattleApplication : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<BattlePresenter>() == null)
            {
                gameObject.AddComponent<BattlePresenter>();
            }
        }
    }
}

