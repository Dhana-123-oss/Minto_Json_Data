using Xunit;
namespace Minto.Api.Tests;
public sealed class OrderWorkflowTests { [Fact] public void Workflow_contract_contains_delivery_stages() { Assert.Contains("Delivered", Enum.GetNames(typeof(Minto.Api.Domain.OrderStatus))); Assert.Contains("OutForDelivery", Enum.GetNames(typeof(Minto.Api.Domain.OrderStatus))); } }
