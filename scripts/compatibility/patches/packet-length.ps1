# RT-F397: outgoing byte-array writes retain their full int lengths.
# Input parsing/received-packet constructors and wire bytes remain unchanged.
$packetLengthTransform = {
    param([string]$text)
    foreach ($site in @('_size += (ushort)bArray.Length;', '_size += (ushort)count;')) {
        if ([regex]::Matches($text, [regex]::Escape($site)).Count -ne 1) { throw "Expected one original byte-array length site: $site" }
        $text = $text.Replace($site, $site.Replace('(ushort)', ''))
    }
    $text
}
