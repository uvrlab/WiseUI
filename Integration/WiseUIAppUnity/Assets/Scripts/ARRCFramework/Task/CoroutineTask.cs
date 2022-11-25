using System.Collections;

namespace ARRC.Framework
{
    public class CoroutineTask : Task
    {
        protected ARRCGenerator gen;
        protected IEnumerator coroutineFunc;

        public CoroutineTask(string title, ARRCGenerator gen, IEnumerator coroutineFunc) : base(title)
        {
            taskType = TaskType.Coroutine;
            this.gen = gen;
            this.coroutineFunc = coroutineFunc;
        }
 
        public override void Start()
        {
            base.Start();
            gen.InitializeState(); // generation 상태를 초기화 한다.
        }

        public override void Enter()
        {
            //currentState = gen.currentState; //  실시간 상태 업데이트.
            progress = gen.progress;
            processingTime = gen.averageTime;

            totalSize = gen.totalCount;

            if (gen.isComplete) // 완료에 의한 호출.
                Dispose();
                
        }

        public override void Dispose() //강제 취소 or 완료에 의한 호출.
        {
            gen.Dispose();
            isCompleted = true; //취소에 의한 경우도 complete로 처리함.
        }
    }
}
