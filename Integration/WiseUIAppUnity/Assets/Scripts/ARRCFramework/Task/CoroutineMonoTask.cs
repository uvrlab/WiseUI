using System.Collections;
using UnityEngine;

namespace ARRC.Framework
{
    public class CoroutineMonoTask : CoroutineTask
    {
        MonoBehaviour parent;
        Coroutine loggerCoroutine;

        public CoroutineMonoTask(string title, MonoBehaviour parent, ARRCGenerator gen, IEnumerator coroutineFunc) : base(title, gen, coroutineFunc)
        {
            this.parent = parent;
        }

        public override void Start()
        {
            base.Start();
            loggerCoroutine = parent.StartCoroutine(coroutineFunc); 
        }
     
        public override void Dispose() 
        {
            // 코루틴 동작 중 취소에 의한 강제 종료.
            // 주의 코루틴 내 yield 이전 해제 되지 않은 메모리 누수있을 수 있음.
            if (!gen.isComplete && loggerCoroutine != null)  
                parent.StopCoroutine(loggerCoroutine);

            base.Dispose();
        }
    }
}
