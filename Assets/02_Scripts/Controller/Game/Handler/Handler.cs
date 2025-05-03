namespace Afterlife.Controller
{
    public class Handler
    {
        protected readonly Controller controller;

        public Handler(Controller controller)
        {
            this.controller = controller;
        }

        public virtual void SetUp() { }
        public virtual void TearDown() { }
        public virtual void Update(float deltaTime) { }
    }
}