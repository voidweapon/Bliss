namespace Bliss
{
    public interface ISessionMessage : IMessage
    {
        
    }
    
    public interface ISessionRequest : IRequest, ISessionMessage
    {
        
    }
    
    public interface ISessionResponse : IResponse, ISessionMessage
    {
        
    }
}