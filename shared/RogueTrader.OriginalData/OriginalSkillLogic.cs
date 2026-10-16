namespace RogueTrader.OriginalData;

/// <summary>
/// Parser for the ITM f[5] "logic" strings (RT-F034). Syntax recovered from an audit of the 200 pinned Rogue_mp.itm rows (53 distinct
/// strings): decimal skill ids, <c>|</c>, <c>&amp;</c>, prefix <c>!</c>, parentheses, <c>%n</c> references, an implicit operator
/// between a parenthesised group and a following term (<c>(2|8)!%1</c>), and the empty string. Evaluator semantics are NOT validated
/// against the original evaluator: treating the implicit operator as AND, the empty string as "no restriction" and <c>%n</c> as a
/// caller-supplied predicate are inferences, and no evidence mixes <c>|</c> with <c>&amp;</c> at one level, so the parser rejects that
/// instead of choosing a precedence. The source text is always kept verbatim.
/// </summary>
public abstract record SkillLogicNode
{
    public abstract bool Evaluate(Func<int, bool> hasSkill, Func<int, bool> percentRef);
    public abstract override string ToString();
}

public sealed record SkillLogicEmpty : SkillLogicNode
{
    public override bool Evaluate(Func<int, bool> hasSkill, Func<int, bool> percentRef) => true;
    public override string ToString() => "";
}
public sealed record SkillLogicSkill(int SkillId) : SkillLogicNode
{
    public override bool Evaluate(Func<int, bool> hasSkill, Func<int, bool> percentRef) => hasSkill(SkillId);
    public override string ToString() => SkillId.ToString();
}
public sealed record SkillLogicPercent(int Index) : SkillLogicNode
{
    public override bool Evaluate(Func<int, bool> hasSkill, Func<int, bool> percentRef) => percentRef(Index);
    public override string ToString() => "%" + Index;
}
public sealed record SkillLogicNot(SkillLogicNode Operand) : SkillLogicNode
{
    public override bool Evaluate(Func<int, bool> hasSkill, Func<int, bool> percentRef) => !Operand.Evaluate(hasSkill, percentRef);
    public override string ToString() => "!" + Operand;
}
public sealed record SkillLogicGroup(SkillLogicNode Inner) : SkillLogicNode
{
    public override bool Evaluate(Func<int, bool> hasSkill, Func<int, bool> percentRef) => Inner.Evaluate(hasSkill, percentRef);
    public override string ToString() => "(" + Inner + ")";
}
public enum SkillLogicOp { Or, And, Implicit }
/// <summary>A flat chain of terms joined by one kind of operator (<c>|</c> alone, or <c>&amp;</c>/implicit). Implicit joins evaluate as AND (inferred).</summary>
public sealed record SkillLogicChain(IReadOnlyList<SkillLogicNode> Terms, IReadOnlyList<SkillLogicOp> Ops) : SkillLogicNode
{
    public override bool Evaluate(Func<int, bool> hasSkill, Func<int, bool> percentRef) =>
        Ops.Count > 0 && Ops[0] == SkillLogicOp.Or
            ? Terms.Any(t => t.Evaluate(hasSkill, percentRef))
            : Terms.All(t => t.Evaluate(hasSkill, percentRef));
    public override string ToString()
    {
        var s = Terms[0].ToString();
        for (var i = 0; i < Ops.Count; i++)
            s += (Ops[i] == SkillLogicOp.Or ? "|" : Ops[i] == SkillLogicOp.And ? "&" : "") + Terms[i + 1];
        return s;
    }
}

public sealed class SkillLogicException(string message) : OriginalDataException(message);

public static class OriginalSkillLogic
{
    /// <summary>Parses <paramref name="text"/> (quotes already removed). The result's <c>ToString()</c> reproduces the input exactly.</summary>
    public static SkillLogicNode Parse(string text)
    {
        if (text.Length == 0) return new SkillLogicEmpty();
        var p = new Parser(text);
        var node = p.Expr();
        if (p.Pos != text.Length) throw p.Fail("unexpected character");
        return node;
    }

    private sealed class Parser(string s)
    {
        public int Pos;
        public SkillLogicException Fail(string why) => new($"skill logic '{s}' at {Pos}: {why}.");

        public SkillLogicNode Expr()
        {
            var terms = new List<SkillLogicNode> { Unary() };
            var ops = new List<SkillLogicOp>();
            while (Pos < s.Length && s[Pos] != ')')
            {
                SkillLogicOp op;
                if (s[Pos] == '|') { op = SkillLogicOp.Or; Pos++; }
                else if (s[Pos] == '&') { op = SkillLogicOp.And; Pos++; }
                else op = SkillLogicOp.Implicit;
                ops.Add(op);
                terms.Add(Unary());
            }
            if (ops.Count == 0) return terms[0];
            var hasOr = ops.Contains(SkillLogicOp.Or);
            if (hasOr && ops.Any(o => o != SkillLogicOp.Or))
                throw Fail("'|' mixed with '&' or an implicit operator at one level; precedence is not evidenced, add parentheses");
            return new SkillLogicChain(terms, ops);
        }

        private SkillLogicNode Unary()
        {
            if (Pos >= s.Length) throw Fail("unexpected end");
            var c = s[Pos];
            if (c == '!') { Pos++; return new SkillLogicNot(Unary()); }
            if (c == '(')
            {
                Pos++;
                var inner = Expr();
                if (Pos >= s.Length || s[Pos] != ')') throw Fail("missing ')'");
                Pos++;
                return new SkillLogicGroup(inner);
            }
            if (c == '%') { Pos++; return new SkillLogicPercent(Number()); }
            if (c is >= '0' and <= '9') return new SkillLogicSkill(Number());
            throw Fail($"unexpected '{c}'");
        }

        private int Number()
        {
            var start = Pos;
            while (Pos < s.Length && s[Pos] is >= '0' and <= '9') Pos++;
            if (start == Pos) throw Fail("digits expected");
            if (s[start] == '0' && Pos - start > 1) throw Fail("leading zero (would not round-trip)");
            return int.Parse(s.AsSpan(start, Pos - start));
        }
    }
}
