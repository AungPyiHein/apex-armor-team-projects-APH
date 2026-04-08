namespace POS.Frontend.Shared;

public static class CategoryIconHelper
{
    public static string GetIcon(string? categoryName)
    {
        return categoryName?.ToLowerInvariant() switch
        {
            // Food & Drink (Cafe / Restaurant)
            "coffee" => "☕",
            "tea" => "🍵",
            "juice" => "🧃",
            "smoothie" => "🍹",
            "ice cream" => "🍨",
            "pastry" => "🥐",
            "sandwich" => "🥪",
            "pizza" => "🍕",
            "sushi" => "🍣",
            "noodles" => "🍜",
            "rice" => "🍚",
            "curry" => "🍛",
            "steak" => "🥩",
            "seafood" => "🦞",
            "salad" => "🥗",
            "soup" => "🍲",
            "bbq" => "🍗",
            "tacos" => "🌮",
            "kebab" => "🥙",
            "dessert" => "🍰",
            "beverages" => "🥤",
            "burgers" => "🍔",
            "milktea" => "🧋",
            "cookies" => "🍪",
            "bread" => "🥖",
            "cake" => "🎂",
            "snacks" => "🍟",

            // Groceries / Fresh Produce
            "fruits" => "🍎",
            "vegetables" => "🥦",
            "meat" => "🥩",
            "fish" => "🐟",

            // Electronics 
            "electronics" => "💻",
            "washing machine" => "🧺",
            "speaker" => "🔊",
            "fridge" => "🧊",
            "smartphone" => "📱",
            "laptop" => "💻",
            "tablet" => "📱",
            "printer" => "🖨️",
            "headphone" => "🎧",
            "monitor" => "🖥️",
            "keyboard" => "⌨️",
            "mouse" => "🖱️",
            "router" => "📡",
            "camera" => "📷",
            "usb cable" => "🔌",
            "selfie stick" => "🤳",
            "battery and charger" => "🔋",
            "ups" => "🔋",
            "fan" => "🌀",
            "air conditioner" => "❄️",
            "vacuum cleaner" => "🧹",
            "projector" => "📽️",
            "gaming" => "🎮",
            "storage" => "💾",
            "tv" => "📺",
            "audio" => "🎵",

            // Fashion & Apparel
            "apparel" => "👕",
            "clothing" => "👕",
            "accessories" => "🧢",
            "hats" => "🎩",
            "shirts" => "👕",
            "pants" => "👖",
            "jackets" => "🧥",
            "skirts" => "👗",
            "shorts" => "🩳",
            "socks" => "🧦",
            "ties" => "👔",
            "sweaters" => "🧶",
            "coats" => "🧥",
            "shoes" => "👟",
            "bags" => "👜",
            "jewelry" => "💍",
            "dress" => "👗",
            "watches" => "⌚",
            "sunglasses" => "🕶️",

            // General / Miscellaneous
            "books" => "📚",
            "appliances" => "🔌",
            "cooking" => "🍳",
            "stationery" => "📏",
            "toys" => "🧸",
            "furniture" => "🛋️",
            "kitchenware" => "🥣",
            "tools" => "🛠️",
            "sports" => "⚽",
            "health" => "💊",
            "pharmacy" => "💊",
            "beauty" => "💄",
            "cosmetics" => "💄",
            "travel" => "🧳",
            "pets" => "🐶",
            "plants" => "🪴",
            "keychains" => "🔑",

            // Default
            _ => "📦"
        };
    }
}
