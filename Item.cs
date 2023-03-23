namespace ITS291; 

public record Item(string Name, decimal Price) {
    public void Deconstruct(out string name, out decimal price) {
        name = Name;
        price = Price;
    }
}
