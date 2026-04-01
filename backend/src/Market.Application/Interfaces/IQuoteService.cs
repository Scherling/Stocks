using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;

namespace Market.Application.Interfaces;

public interface IQuoteService
{
    Task<QuoteResponse> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default);
}
