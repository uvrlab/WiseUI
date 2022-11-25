using System.Collections;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

namespace ARRC.Framework
{
    public class CoroutineEditorTask : CoroutineTask
    {
        EditorWindow editorWnd;
        EditorCoroutine loggerCoroutine;

        public CoroutineEditorTask(string title, EditorWindow editorWnd, ARRCGenerator gen, IEnumerator coroutineFunc) : base(title, gen, coroutineFunc)
        {
            this.editorWnd = editorWnd;
        }
  
        public override void Start()
        {
            base.Start();
            loggerCoroutine = editorWnd.StartCoroutine(coroutineFunc);
        }

        public override void Dispose()
        {
            // 코루틴 동작 중 취소에 의한 강제 종료.
            // 주의 코루틴 내 yield 이전 해제 되지 않은 메모리 누수있을 수 있음.
            if (!gen.isComplete) 
                editorWnd.StopCoroutine(loggerCoroutine);

            base.Dispose();
        }

    }
}
