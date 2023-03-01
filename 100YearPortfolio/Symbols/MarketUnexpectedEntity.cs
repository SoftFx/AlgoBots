namespace _100YearPortfolio.Symbols
{
    internal sealed record UnexpectedEntity(string Id, string Symbol, string Type)
    {
        public override string ToString()
        {
            return $"{Symbol} {Type} Id={Id}";
        }
    }
}
