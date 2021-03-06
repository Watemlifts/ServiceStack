﻿using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Model;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class DtoUtils
    {
        /// <summary>
        /// Naming convention for the ResponseStatus property name on the response DTO
        /// </summary>
        public const string ResponseStatusPropertyName = "ResponseStatus";

        public static ResponseStatus ToResponseStatus(this Exception exception)
        {
            return exception is IResponseStatusConvertible customStatus
                ? customStatus.ToResponseStatus()
                : CreateResponseStatus(exception.GetType().Name, exception.Message);
        }

        public static ResponseStatus ToResponseStatus(this ValidationError validationException)
        {
            return ResponseStatusUtils.CreateResponseStatus(validationException.ErrorCode, validationException.Message, validationException.Violations);
        }

        public static ResponseStatus ToResponseStatus(this ValidationErrorResult validationResult)
        {
            return validationResult.IsValid
                ? CreateSuccessResponse(validationResult.SuccessMessage)
                : ResponseStatusUtils.CreateResponseStatus(validationResult.ErrorCode, validationResult.ErrorMessage, validationResult.Errors);
        }

        public static ResponseStatus CreateSuccessResponse(string message)
        {
            return new ResponseStatus { Message = message };
        }

        public static ResponseStatus CreateResponseStatus(string errorCode)
        {
            var errorMessage = errorCode.SplitCamelCase();
            return ResponseStatusUtils.CreateResponseStatus(errorCode, errorMessage, null);
        }

        public static ResponseStatus CreateResponseStatus(string errorCode, string errorMessage)
        {
            return ResponseStatusUtils.CreateResponseStatus(errorCode, errorMessage, null);
        }

        public static object CreateErrorResponse(string errorCode, string errorMessage, IEnumerable<ValidationErrorField> validationErrors)
        {
            var responseStatus = ResponseStatusUtils.CreateResponseStatus(errorCode, errorMessage, validationErrors);
            var responseDto = CreateResponseDto(null, responseStatus);
            return new HttpError(responseDto, HttpStatusCode.BadRequest, errorCode, errorMessage);
        }

        public static object CreateErrorResponse(object request, ValidationErrorResult validationError)
        {
            var responseStatus = validationError.ToResponseStatus();

            var errorResponse = CreateErrorResponse(
                request,
                new ValidationError(validationError),
                responseStatus);

            return errorResponse;
        }

        public static object CreateErrorResponse(object request, Exception ex, ResponseStatus responseStatus)
        {
            var responseDto = CreateResponseDto(request, responseStatus);

            if (ex is IHttpError httpError)
            {
                if (responseDto != null)
                    httpError.Response = responseDto;

                return httpError;
            }

            var errorCode = ex.GetType().Name;
            var errorMsg = ex.Message;
            if (responseStatus != null)
            {
                errorCode = responseStatus.ErrorCode ?? errorCode;
                errorMsg = responseStatus.Message ?? errorMsg;
            }

            return new HttpError(responseDto, ex.ToStatusCode(), errorCode, errorMsg, ex);
        }

        /// <summary>
        /// Create an instance of the service response dto type and inject it with the supplied responseStatus
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStatus"></param>
        /// <returns></returns>
        public static object CreateResponseDto(object request, ResponseStatus responseStatus)
        {
            // Predict the Response message type name
            var responseDtoType = WebRequestUtils.GetErrorResponseDtoType(request);
            var responseDto = responseDtoType.CreateInstance();
            if (responseDto == null)
                return null;

            // For faster serialization of exceptions, services should implement IHasResponseStatus
            if (responseDto is IHasResponseStatus hasResponseStatus)
            {
                hasResponseStatus.ResponseStatus = responseStatus;
            }
            else
            {
                var responseStatusProperty = responseDtoType.GetProperty(ResponseStatusPropertyName);
                // Set the ResponseStatus
                responseStatusProperty?.SetProperty(responseDto, responseStatus);
            }

            // Return an Error DTO with the exception populated
            return responseDto;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iocResolver"></param>
        /// <param name="request"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static object CreateErrorResponse(object request, Exception ex)
        {
            var originalEx = ex;
            ex = HostContext.AppHost?.ResolveResponseException(ex) ?? ex;
            var responseStatus = ex.ToResponseStatus();

            //If ResponseStatus is null fallback to use original Exception
            if (responseStatus == null)
            {
                ex = originalEx;
                responseStatus = ex.ToResponseStatus();
            }

            if (HostContext.DebugMode)
            {
                // View stack trace in tests and on the client
                responseStatus.StackTrace = GetRequestErrorBody(request) + "\n" + ex;
            }

            HostContext.AppHost?.OnLogError(typeof(DtoUtils), "ServiceBase<TRequest>::Service Exception", ex);

            var errorResponse = CreateErrorResponse(request, ex, responseStatus);

            HostContext.AppHost?.OnExceptionTypeFilter(ex, responseStatus);

            return errorResponse;
        }

        /// <summary>
        /// Override to provide additional/less context about the Service Exception. 
        /// By default the request is serialized and appended to the ResponseStatus StackTrace.
        /// </summary>
        public static string GetRequestErrorBody(object request)
        {
            var requestString = "";
            try
            {
                requestString = TypeSerializer.SerializeToString(request);
            }
            catch /*(Exception ignoreSerializationException)*/
            {
                //Serializing request successfully is not critical and only provides added error info
            }

            return $"[{(request ?? new object()).GetType().GetOperationName()}: {DateTime.UtcNow}]:\n[REQUEST: {requestString}]";
        }
    }
}