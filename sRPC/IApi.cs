namespace sRPC
{
    /// <summary>
    /// The interface that can provide a <typeparamref name="T"/> API.
    /// </summary>
    /// <typeparam name="T">the type of the API</typeparam>
    public interface IApi<T>
    {
        /// <summary>
        /// The current API interface.
        /// </summary>
        T Api { get; }
    }

    /// <summary>
    /// The interface that can provide a <typeparamref name="TRequest"/> and
    /// <typeparamref name="TResponse"/> API.
    /// </summary>
    /// <typeparam name="TRequest">the type of the API for creating requests</typeparam>
    /// <typeparam name="TResponse">the type of the API for responding</typeparam>
    public interface IApi<TRequest, TResponse>
    {
        /// <summary>
        /// The current API interface to create requests
        /// </summary>
        TRequest RequestApi { get; }

        /// <summary>
        /// The current API interface to respond to requests
        /// </summary>
        TResponse ResponseApi { get; }
    }
}
