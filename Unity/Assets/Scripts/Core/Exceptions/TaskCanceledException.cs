using System;
using ET;

namespace Bliss
{
    /// <summary>
    /// 表示任务因协作式取消而被取消。调用方可按 <see cref="OperationCanceledException"/> 处理并停止当前操作。
    /// </summary>
    public class TaskCanceledException : TaskAbortedException
    {
        /// <summary>创建取消异常。</summary>
        public TaskCanceledException()
        {
        }

        /// <summary>创建包含取消原因的异常。</summary>
        /// <param name="message">取消原因。</param>
        public TaskCanceledException(string message) : base(message)
        {
        }

        /// <summary>创建包含内部原因的取消异常。</summary>
        /// <param name="message">取消原因。</param>
        /// <param name="innerException">内部异常。</param>
        public TaskCanceledException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>创建关联协作式取消令牌的异常。</summary>
        /// <param name="message">取消原因。</param>
        /// <param name="cancellationToken">导致取消的协作式令牌。</param>
        public TaskCanceledException(string message, ETCancellationToken cancellationToken) : base(message, cancellationToken)
        {
        }
    }

    /// <summary>
    /// 表示任务因协作式取消而被取消；可通过 <see cref="ETCancellationToken"/> 确认取消来源。
    /// </summary>
    /// <remarks>
    /// Callers should propagate this cancellation or stop the canceled operation, rather than treating it as a business failure.
    /// </remarks>
    public class TaskAbortedException : OperationCanceledException
    {
        /// <summary>创建取消异常。</summary>
        public TaskAbortedException()
        {
        }

        /// <summary>创建包含取消原因的异常。</summary>
        /// <param name="message">取消原因。</param>
        public TaskAbortedException(string message) : base(message)
        {
        }

        /// <summary>创建包含内部原因的取消异常。</summary>
        /// <param name="message">取消原因。</param>
        /// <param name="innerException">内部异常。</param>
        public TaskAbortedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>创建关联协作式取消令牌的异常。</summary>
        /// <param name="message">取消原因。</param>
        /// <param name="cancellationToken">导致取消的协作式令牌。</param>
        public TaskAbortedException(string message, ETCancellationToken cancellationToken) : base(message)
        {
            ETCancellationToken = cancellationToken;
        }

        /// <summary>导致取消的协作式令牌；未知来源时为 null。</summary>
        public ETCancellationToken ETCancellationToken { get; }
    }

    /// <summary>表示任务超过时间限制；调用方可按 <see cref="TimeoutException"/> 处理。</summary>
    /// <remarks>Callers should handle this as a timeout failure, distinct from cooperative cancellation.</remarks>
    public class TaskTimeoutException : TimeoutException
    {
        /// <summary>创建超时异常。</summary>
        public TaskTimeoutException()
        {
        }

        /// <summary>创建包含超时原因的异常。</summary>
        /// <param name="message">超时原因。</param>
        public TaskTimeoutException(string message) : base(message)
        {
        }

        /// <summary>创建包含内部原因的超时异常。</summary>
        /// <param name="message">超时原因。</param>
        /// <param name="innerException">内部异常。</param>
        public TaskTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}