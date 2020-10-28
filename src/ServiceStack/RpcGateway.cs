using System;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack
{
    public class RpcGateway
    {
        private ServiceStackHost AppHost { get; }
        private IServiceExecutor Executor { get; }

        public RpcGateway(ServiceStackHost appHost) : this(appHost, appHost.ServiceController) {}

        public RpcGateway(ServiceStackHost appHost, IServiceExecutor executor)
        {
            AppHost = appHost ?? throw new ArgumentNullException(nameof(appHost));
            Executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public virtual async Task<TResponse> ExecuteAsync<TResponse>(object requestDto, IRequest req)
        {
            try 
            {
                AppHost.AssertFeatures(Feature.Grpc);

                var res = req.Response;
                if (AppHost.ApplyPreRequestFilters(req, req.Response))
                    return CreateResponse<TResponse>(res);

                await AppHost.ApplyRequestFiltersAsync(req, res, requestDto);
                if (res.IsClosed)
                    return CreateResponse<TResponse>(res);

                var response = await Executor.ExecuteAsync(requestDto, req);

                response = await AppHost.ApplyResponseConvertersAsync(req, response);
            
                await AppHost.ApplyResponseFiltersAsync(req, res, response);
                if (res.IsClosed)
                    return CreateResponse<TResponse>(res);

                if (response == null)
                    return typeof(TResponse).CreateInstance<TResponse>();

                return GetResponse<TResponse>(res, response);
            }
            catch (Exception e)
            {
                return GetResponse<TResponse>(req.Response, e);
            }
        }

        private static TResponse CreateResponse<TResponse>(IResponse res)
        {
            if (res.Dto != null)
                return GetResponse<TResponse>(res, res.Dto);

            if (res.StatusCode >= 300)
            {
                return GetResponse<TResponse>(res, CreateError(res));
            }
            
            return typeof(TResponse).CreateInstance<TResponse>();
        }
        
        private static TResponse GetResponse<TResponse>(IResponse res, object ret)
        {
            if (ret is Exception ex)
            {
                if (res.StatusCode < 300)
                    res.StatusCode = ex.ToStatusCode();
                
                var status = (ret is IHttpError httpError
                         ? httpError.GetResponseStatus()
                         : null)
                     ?? ex.ToResponseStatus();

                var instance = typeof(TResponse).CreateInstance<TResponse>();
                if (instance is IHasResponseStatus hasStatus)
                    hasStatus.ResponseStatus = status;
                else
                {
                    var pi = TypeProperties<TResponse>.GetAccessor(nameof(IHasResponseStatus.ResponseStatus));
                    if (pi != null)
                        pi.PublicSetter(instance, status);
                    else throw ex; // can't inject ResponseStatus treat as generic unstructured handler error
                }

                return instance;
            }
            if (ret is IHttpResult httpResult)
                return (TResponse) httpResult.Response;

            return (TResponse) ret;
        }

        public static HttpError CreateError(IResponse res, string errorCode=null, string errorMessage=null)
        {
            if (errorCode == null)
            {
                errorCode = res.StatusCode.ToString();
                if (Enum.IsDefined(typeof(HttpStatusCode), res.StatusCode))
                {
                    var httpStatus = (HttpStatusCode) res.StatusCode;
                    errorCode = httpStatus.ToString();
                }
            }
            var to = new HttpError(res.StatusCode, errorCode, errorMessage ?? res.StatusDescription);
            return to;
        }
    }
}