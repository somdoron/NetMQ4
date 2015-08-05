namespace NetMQ.Core
{
    internal interface IMailbox
    {
        void Send(Command command);
    }
}