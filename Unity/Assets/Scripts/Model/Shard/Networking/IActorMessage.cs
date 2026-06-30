namespace Bliss
{
    public interface IActorMessage : IMessage
    {
        long ActorId
        {
            get;
            set;
        }
    }
    
    public interface IActorRequestMessage : IActorMessage, IRequest
    {

    }
    
    public interface IActorResponseMessage : IActorMessage, IResponse
    {

    }
}