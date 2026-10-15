# RT-F390. Apply to an unchanged pinned ScriptArena.cs string in a derived copy only.
# Export a scriptblock so prepare-derived-source.ps1 can compose same-file patches
# and record the original/derived hashes once.
$nullScriptCapacityTransform = {
    param([string]$text)

    $sportTypes = @('GameType_SoccerBrawl', 'GameType_Gravball', 'GameType_BasketBall', 'GameType_BoomBall')
    foreach ($sportType in $sportTypes) {
        $original = '_scriptType.Equals("' + $sportType + '", StringComparison.OrdinalIgnoreCase)'
        if ([regex]::Matches($text, [regex]::Escape($original)).Count -ne 1) {
            throw "Expected exactly one original capacity comparison for $sportType."
        }
        $replacement = 'string.Equals(_scriptType, "' + $sportType + '", StringComparison.OrdinalIgnoreCase)'
        $text = $text.Replace($original, $replacement)
    }

    # Retain all four case-insensitive sport queue exceptions and the original
    # capacity threshold/message. Static Equals treats a null generic binding
    # exactly like any other non-sport binding instead of dereferencing it.
    $text
}
