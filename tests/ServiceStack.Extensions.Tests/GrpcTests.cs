﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using Funq;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests
{
    [ServiceContract(Name = "Hyper.Calculator")]
    public interface ICalculator
    {
        ValueTask<MultiplyResult> MultiplyAsync(MultiplyRequest request);
    }

    [DataContract]
    public class MultiplyRequest
    {
        [DataMember(Order = 1)]
        public int X { get; set; }

        [DataMember(Order = 2)]
        public int Y { get; set; }
    }

    [DataContract]
    public class MultiplyResult
    {
        [DataMember(Order = 1)]
        public int Result { get; set; }
    }
    
//    [ServiceContract]
//    public interface ITimeService
//    {
//        IAsyncEnumerable<TimeResult> SubscribeAsync(CallContext context = default);
//    }

//    [ProtoContract]
//    public class TimeResult
//    {
//        [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
//        public DateTime Time { get; set; }
//    }
    
    public class MyCalculator : ICalculator
    {
        ValueTask<MultiplyResult> ICalculator.MultiplyAsync(MultiplyRequest request)
        {
            var result = new MultiplyResult { Result = request.X * request.Y };
            return new ValueTask<MultiplyResult>(result);
        }
    }
    
//    public class MyTimeService : ITimeService
//    {
//        public IAsyncEnumerable<TimeResult> SubscribeAsync(CallContext context = default)
//            => SubscribeAsyncImpl(default); // context.CancellationToken);
//
//        private async IAsyncEnumerable<TimeResult> SubscribeAsyncImpl([EnumeratorCancellation] CancellationToken cancel)
//        {
//            while (!cancel.IsCancellationRequested)
//            {
//                await Task.Delay(TimeSpan.FromSeconds(10));
//                yield return new TimeResult { Time = DateTime.UtcNow };
//            }
//        }
//    }    
    
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCodeFirstGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MyCalculator>();
//                endpoints.MapGrpcService<MyTimeService>();
            });
        }
    }

    public class AppHost : AppSelfHostBase
    {
        private static readonly byte[] AuthKey = AesUtils.CreateKey();
        public const string Username = "mythz";
        public const string Password = "p@55word";

        public AppHost() 
            : base(nameof(GrpcTests), typeof(MyServices).Assembly) { }

        public override void Configure(Container container)
        {
            //BinderConfiguration.Default.Bin
            //ServiceBinder.Default = new GrpcServiceBinder();
         
            Plugins.Add(new ValidationFeature());
            Plugins.Add(new GrpcFeature(App));
        }

        public override void ConfigureKestrel(KestrelServerOptions options)
        {
            options.ListenLocalhost(20000, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        }

        public override void Configure(IServiceCollection services)
        {
            services.AddServiceStackGrpc();
        }

        public override void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MyCalculator>();
            });
        }
    }

    [DataContract]
    public class Multiply : IReturn<MultiplyResponse>
    {
        [DataMember(Order = 1)]
        public int X { get; set; }

        [DataMember(Order = 2)]
        public int Y { get; set; }
    }

    [DataContract]
    public class MultiplyResponse
    {
        [DataMember(Order = 1)]
        public int Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class Incr : IReturnVoid
    {
        internal static int Counter = 0;
        
        [DataMember(Order = 1)]
        public int Amount { get; set; }
    }

    [DataContract]
    public class GetHello : IReturn<HelloResponse>, IGet
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [DataContract]
    public class AnyHello : IReturn<HelloResponse>
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [DataContract]
    public class HelloResponse
    {
        [DataMember(Order = 1)]
        public string Result { get; set; }
        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class Throw : IReturn<HelloResponse>
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }

    [DataContract]
    public class ThrowVoid : IReturnVoid
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }

    [DataContract]
    public class AddHeader : IReturnVoid
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
        [DataMember(Order = 2)]
        public string Value { get; set; }
    }
    
    [DataContract]
    public class TriggerValidators : IReturn<EmptyResponse>
    {
        [DataMember(Order = 1)]
        public string CreditCard { get; set; }
        [DataMember(Order = 2)]
        public string Email { get; set; }
        [DataMember(Order = 3)]
        public string Empty { get; set; }
        [DataMember(Order = 4)]
        public string Equal { get; set; }
        [DataMember(Order = 5)]
        public int ExclusiveBetween { get; set; }
        [DataMember(Order = 6)]
        public int GreaterThanOrEqual { get; set; }
        [DataMember(Order = 7)]
        public int GreaterThan { get; set; }
        [DataMember(Order = 8)]
        public int InclusiveBetween { get; set; }
        [DataMember(Order = 9)]
        public string Length { get; set; }
        [DataMember(Order = 10)]
        public int LessThanOrEqual { get; set; }
        [DataMember(Order = 11)]
        public int LessThan { get; set; }
        [DataMember(Order = 12)]
        public string NotEmpty { get; set; }
        [DataMember(Order = 13)]
        public string NotEqual { get; set; }
        [DataMember(Order = 14)]
        public string Null { get; set; }
        [DataMember(Order = 15)]
        public string RegularExpression { get; set; }
        [DataMember(Order = 16)]
        public decimal ScalePrecision { get; set; }
    }

    public class TriggerValidatorsValidator : AbstractValidator<TriggerValidators>
    {
        public TriggerValidatorsValidator()
        {
            RuleFor(x => x.CreditCard).CreditCard();
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Empty).Empty();
            RuleFor(x => x.Equal).Equal("Equal");
            RuleFor(x => x.ExclusiveBetween).ExclusiveBetween(10, 20);
            RuleFor(x => x.GreaterThanOrEqual).GreaterThanOrEqualTo(10);
            RuleFor(x => x.GreaterThan).GreaterThan(10);
            RuleFor(x => x.InclusiveBetween).InclusiveBetween(10, 20);
            RuleFor(x => x.Length).Length(10);
            RuleFor(x => x.LessThanOrEqual).LessThanOrEqualTo(10);
            RuleFor(x => x.LessThan).LessThan(10);
            RuleFor(x => x.NotEmpty).NotEmpty();
            RuleFor(x => x.NotEqual).NotEqual("NotEqual");
            RuleFor(x => x.Null).Null();
            RuleFor(x => x.RegularExpression).Matches(@"^[a-z]*$");
            RuleFor(x => x.ScalePrecision).SetValidator(new ScalePrecisionValidator(1, 1));
        }
    }


    public class MyServices : Service
    {
        public Task<MultiplyResponse> Post(Multiply request)
        {
            var result = new MultiplyResponse { Result = request.X * request.Y };
            return Task.FromResult(result);
        }

        public void Any(Incr request)
        {
            Incr.Counter += request.Amount;
        }

        public object Get(GetHello request) => new HelloResponse { Result = $"Hello, {request.Name}!" };

        public object Any(AnyHello request) => new HelloResponse { Result = $"Hello, {request.Name}!" };
        
        public object Get(Throw request) => throw new Exception(request.Message ?? "Error in Throw");
        
        public void Get(ThrowVoid request) => throw new Exception(request.Message ?? "Error in ThrowVoid");

        public object Post(TriggerValidators request) => new EmptyResponse();

        public void Get(AddHeader request)
        {
            Response.AddHeader(request.Name, request.Value);
        }
    }
    
    /// <summary>
    /// TODO:
    /// - Exceptions
    /// - Validation
    /// - Auth
    ///   - JWT
    ///   - Basic Auth
    /// - AutoQuery
    /// - Multitenancy? 
    /// </summary>

    public class GrpcTests
    {
        private readonly ServiceStackHost appHost;
        public GrpcTests()
        {
            appHost = new AppHost()
                .Init()
                .Start("http://localhost:20000/");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        private static GrpcServiceClient GetClient()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            var client = new GrpcServiceClient("http://localhost:20000");
            return client;
        }

        [Test]
        public async Task Can_call_MultiplyRequest_Grpc_Service_ICalculator()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            using var http = GrpcChannel.ForAddress("http://localhost:20000");
            var calculator = http.CreateGrpcService<ICalculator>();
            var result = await calculator.MultiplyAsync(new MultiplyRequest { X = 12, Y = 4 });
            Assert.That(result.Result, Is.EqualTo(48));
        }

        [Test]
        public async Task Can_call_Multiply_Grpc_Service_GrpcChannel()
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            using var http = GrpcChannel.ForAddress("http://localhost:20000");

            var response = await http.CreateCallInvoker().Execute<Multiply, MultiplyResponse>(new Multiply { X = 12, Y = 4 }, "GrpcServices",
                HttpMethods.Post.ToPascalCase() + nameof(Multiply));

            Assert.That(response.Result, Is.EqualTo(48));
        }

        [Test]
        public async Task Can_call_Multiply_Grpc_Service_GrpcServiceClient()
        {
            using var client = GetClient();

            var response = await client.PostAsync(new Multiply { X = 12, Y = 4 });
            Assert.That(response.Result, Is.EqualTo(48));
        }

        [Test]
        public async Task Can_call_Incr_ReturnVoid_GrpcServiceClient()
        {
            Incr.Counter = 0;

            using var client = GetClient();

            await client.PublishAsync(new Incr { Amount = 1 });
            Assert.That(Incr.Counter, Is.EqualTo(1));

            await client.PublishAsync(new Incr { Amount = 2 });
            Assert.That(Incr.Counter, Is.EqualTo(3));
        }

        [Test]
        public async Task Can_call_GetHello_with_Get_or_Send()
        {
            using var client = GetClient();

            var response = await client.GetAsync(new GetHello { Name = "GET" });
            Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

            response = await client.SendAsync(new GetHello { Name = "SEND" });
            Assert.That(response.Result, Is.EqualTo($"Hello, SEND!"));
        }

        [Test]
        public async Task Can_call_AnyHello_with_Get_Post_or_Send()
        {
            using var client = GetClient();

            var response = await client.GetAsync(new AnyHello { Name = "GET" });
            Assert.That(response.Result, Is.EqualTo($"Hello, GET!"));

            response = await client.PostAsync(new AnyHello { Name = "POST" });
            Assert.That(response.Result, Is.EqualTo($"Hello, POST!"));

            response = await client.SendAsync(new GetHello { Name = "SEND" });
            Assert.That(response.Result, Is.EqualTo($"Hello, SEND!"));
        }

        [Test]
        public async Task Can_call_AnyHello_Batch()
        {
            using var client = GetClient();

            var requests = new[] {
                new AnyHello {Name = "A"},
                new AnyHello {Name = "B"},
                new AnyHello {Name = "C"},
            };
            var responses = await client.SendAllAsync(requests);
            Assert.That( responses.Map(x => x.Result), Is.EqualTo(new[] {
                $"Hello, A!",
                $"Hello, B!",
                $"Hello, C!",
            }));
        }

        [Test]
        public async Task Can_call_Incr_Batch_ReturnVoid()
        {
            Incr.Counter = 0;
            
            using var client = GetClient();

            var requests = new[] {
                new Incr {Amount = 1},
                new Incr {Amount = 2},
                new Incr {Amount = 3},
            };
            await client.PublishAllAsync(requests);
            
            Assert.That(Incr.Counter, Is.EqualTo(1 + 2 + 3));
        }

        [Test]
        public async Task Does_throw_WebServiceException()
        {
            using var client = GetClient();

            try
            {
                await client.GetAsync(new Throw { Message = "throw test" });
                Assert.Fail("should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(500));
                Assert.That(e.Message, Is.EqualTo("throw test"));
            }
        }

        [Test]
        public async Task Does_throw_WebServiceException_ReturnVoid()
        {
            using var client = GetClient();

            try
            {
                await client.GetAsync(new ThrowVoid { Message = "throw test" });
                Assert.Fail("should throw");
            }
            catch (WebServiceException e)
            {
                Assert.That(e.StatusCode, Is.EqualTo(500));
                Assert.That(e.Message, Is.EqualTo("throw test"));
            }
        }

        [Test]
        public async Task Triggering_all_validators_returns_right_ErrorCode()
        {
            var client = GetClient();
            var request = new TriggerValidators
            {
                CreditCard = "NotCreditCard",
                Email = "NotEmail",
                Empty = "NotEmpty",
                Equal = "NotEqual",
                ExclusiveBetween = 1,
                GreaterThan = 1,
                GreaterThanOrEqual = 1,
                InclusiveBetween = 1,
                Length = "Length",
                LessThan = 20,
                LessThanOrEqual = 20,
                NotEmpty = "",
                NotEqual = "NotEqual",
                Null = "NotNull",
                RegularExpression = "FOO",
                ScalePrecision = 123.456m
            };

            try
            {
                var response = await client.PostAsync(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                //ex.ResponseStatus.PrintDump();
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                var errors = ex.ResponseStatus.Errors;
                Assert.That(errors.First(x => x.FieldName == "CreditCard").ErrorCode, Is.EqualTo("CreditCard"));
                Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
                Assert.That(errors.First(x => x.FieldName == "Email").ErrorCode, Is.EqualTo("Email"));
                Assert.That(errors.First(x => x.FieldName == "Empty").ErrorCode, Is.EqualTo("Empty"));
                Assert.That(errors.First(x => x.FieldName == "Equal").ErrorCode, Is.EqualTo("Equal"));
                Assert.That(errors.First(x => x.FieldName == "ExclusiveBetween").ErrorCode, Is.EqualTo("ExclusiveBetween"));
                Assert.That(errors.First(x => x.FieldName == "GreaterThan").ErrorCode, Is.EqualTo("GreaterThan"));
                Assert.That(errors.First(x => x.FieldName == "GreaterThanOrEqual").ErrorCode, Is.EqualTo("GreaterThanOrEqual"));
                Assert.That(errors.First(x => x.FieldName == "InclusiveBetween").ErrorCode, Is.EqualTo("InclusiveBetween"));
                Assert.That(errors.First(x => x.FieldName == "Length").ErrorCode, Is.EqualTo("Length"));
                Assert.That(errors.First(x => x.FieldName == "LessThan").ErrorCode, Is.EqualTo("LessThan"));
                Assert.That(errors.First(x => x.FieldName == "LessThanOrEqual").ErrorCode, Is.EqualTo("LessThanOrEqual"));
                Assert.That(errors.First(x => x.FieldName == "NotEmpty").ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(errors.First(x => x.FieldName == "NotEqual").ErrorCode, Is.EqualTo("NotEqual"));
                Assert.That(errors.First(x => x.FieldName == "Null").ErrorCode, Is.EqualTo("Null"));
                Assert.That(errors.First(x => x.FieldName == "RegularExpression").ErrorCode, Is.EqualTo("RegularExpression"));
                Assert.That(errors.First(x => x.FieldName == "ScalePrecision").ErrorCode, Is.EqualTo("ScalePrecision"));
            }
        }

        [Test]
        public async Task Does_return_Custom_Headers()
        {
            var client = GetClient();
            string customHeader = null;
            client.ResponseFilter = ctx => customHeader = ctx.GetHeader("X-Custom");

            await client.GetAsync(new AddHeader { Name = "X-Custom", Value = "A" });
            Assert.That(customHeader, Is.EqualTo("A"));
        }
    }
}