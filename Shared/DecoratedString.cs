public abstract class DecoratedString
{
    public abstract string PlainText();

    public virtual string StyledText() => PlainText();

    public static DecoratedString operator +(DecoratedString left, DecoratedString right) => new ConcatString(left, right);

    public static implicit operator DecoratedString(string str) => new PlainString(str);
}

class PlainString : DecoratedString
{
    private readonly string str;

    public PlainString(string str) => this.str = str;

    public override string PlainText() => str;
}

class ConcatString : DecoratedString
{
    private readonly DecoratedString left;
    private readonly DecoratedString right;

    public ConcatString(DecoratedString left, DecoratedString right)
    {
        this.left = left;
        this.right = right;
    }

    public override string PlainText() => left.PlainText() + right.PlainText();

    public override string StyledText() => left.StyledText() + right.StyledText();
}

abstract class StyledString : DecoratedString
{
    protected readonly DecoratedString str;

    protected StyledString(string str)
        : this(new PlainString(str)) { }

    protected StyledString(DecoratedString str) => this.str = str;

    public override string PlainText() => str.PlainText();
}

class RedString : StyledString
{
    public RedString(string str)
        : base(str) { }

    public RedString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[31m{str.StyledText()}\u001b[39m";
}

class GreenString : StyledString
{
    public GreenString(string str)
        : base(str) { }

    public GreenString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[32m{str.StyledText()}\u001b[39m";
}

class OrangeString : StyledString
{
    public OrangeString(string str)
        : base(str) { }

    public OrangeString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[33m{str.StyledText()}\u001b[39m";
}

class BlueString : StyledString
{
    public BlueString(string str)
        : base(str) { }

    public BlueString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[34m{str.StyledText()}\u001b[39m";
}

class PurpleString : StyledString
{
    public PurpleString(string str)
        : base(str) { }

    public PurpleString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[35m{str.StyledText()}\u001b[39m";
}

class CyanString : StyledString
{
    public CyanString(string str)
        : base(str) { }

    public CyanString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[36m{str.StyledText()}\u001b[39m";
}

class UnderlineString : StyledString
{
    public UnderlineString(string str)
        : base(str) { }

    public UnderlineString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[4m{str.StyledText()}\u001b[24m";
}

class DoubleUnderlineString : StyledString
{
    public DoubleUnderlineString(string str)
        : base(str) { }

    public DoubleUnderlineString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[21m{str.StyledText()}\u001b[24m";
}

class BoldString : StyledString
{
    public BoldString(string str)
        : base(str) { }

    public BoldString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[1m{str.StyledText()}\u001b[22m";
}

class ItalicString : StyledString
{
    public ItalicString(string str)
        : base(str) { }

    public ItalicString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[3m{str.StyledText()}\u001b[23m";
}

class StrikethroughString : StyledString
{
    public StrikethroughString(string str)
        : base(str) { }

    public StrikethroughString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[9m{str.StyledText()}\u001b[29m";
}

class SlowBlinkString : StyledString
{
    public SlowBlinkString(string str)
        : base(str) { }

    public SlowBlinkString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[5m{str.StyledText()}\u001b[25m";
}

class RapidBlinkString : StyledString
{
    public RapidBlinkString(string str)
        : base(str) { }

    public RapidBlinkString(DecoratedString str)
        : base(str) { }

    public override string StyledText() => $"\u001b[6m{str.StyledText()}\u001b[25m";
}
