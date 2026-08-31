// The same dictionary TranslationMediator reads, for text whose key is not known until it is on
// screen. A key binding's face is whatever the player pressed last, so no component can carry that
// key in a serialized field - but the string still has to come out of the translation file.
//
// Anything without an entry comes back as itself and says nothing about it. A bare key name like
// "A" or "F5" reads the same in every language and is not worth an entry per letter; the words -
// "Space", "Left mouse", "Numpad 1" - are the ones the files carry.
public static class TranslationLookup
{
    public static string Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        string translation;
        return TranslationJSONDeserializer.dataDictionary.TryGetValue(key, out translation) ? translation : key;
    }
}
