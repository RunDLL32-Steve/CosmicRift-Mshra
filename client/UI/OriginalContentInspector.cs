using Godot;

namespace RogueTrader.Client;

/// <summary>Asset inspection only: no world state, input commands or simulation ticks.</summary>
public partial class OriginalContentInspector : CanvasLayer
{
    public event Action? CloseRequested;
    private readonly OriginalSpriteLibrary? _library;
    private OriginalAssetCanvas _canvas = null!;
    private SpinBox _row = null!;
    private SpinBox _column = null!;
    private OptionButton _selector = null!;
    private HBoxContainer _frameControls = null!;
    private string _page = "ships";
    private readonly Dictionary<string, (int Row, int Column)> _frames = [];
    private bool _updatingFrameControls;
    public int Row => _canvas.Row;
    public int Column => _canvas.Column;
    public int SelectedSprite => _canvas.Selected;
    private string FrameKey => _page == "ships" ? "ships" : $"sprite:{_canvas.Selected}";
    public string Page => _page;

    public OriginalContentInspector(OriginalSpriteLibrary? library) { _library = library; Layer = 20; }

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(root);
        var background = new ColorRect { Color = new Color("070b10"), MouseFilter = Control.MouseFilterEnum.Stop };
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(background);
        var title = MakeLabel("COSMIC RIFT  /  ROGUE TRADER ORIGINAL CONTENT", 23);
        title.Position = new Vector2(28, 16);
        root.AddChild(title);
        var note = MakeLabel("World paused · preserved archive pixels · native appearance rules under verification", 13);
        note.Position = new Vector2(28, 47);
        note.Modulate = new Color("97adb8");
        root.AddChild(note);
        var nav = new HBoxContainer { Position = new Vector2(28, 73) };
        nav.AddThemeConstantOverride("separation", 10);
        root.AddChild(nav);
        foreach (var (page, text) in new[] { ("ships", "12 HULLS"), ("sprites", "SPRITE ARCHIVES"), ("map", "ROGUE MAP") })
        {
            var button = new Button { Text = text, Name = "Inspect_" + page, CustomMinimumSize = new Vector2(145, 33) };
            button.Pressed += () => ShowPage(page);
            nav.AddChild(button);
        }
        var close = new Button { Text = "CLOSE  /  ESC", Name = "Inspect_close", CustomMinimumSize = new Vector2(160, 33) };
        close.Pressed += () => CloseRequested?.Invoke();
        nav.AddChild(close);
        _frameControls = new HBoxContainer { Position = new Vector2(746, 73) };
        _frameControls.AddThemeConstantOverride("separation", 12);
        root.AddChild(_frameControls);
        _frameControls.AddChild(MakeLabel("ROW", 13));
        _row = new SpinBox { Name = "Inspect_row", MinValue = 0, MaxValue = 63, Step = 1, Value = 16, CustomMinimumSize = new Vector2(110, 33) };
        _frameControls.AddChild(_row);
        _frameControls.AddChild(MakeLabel("COLUMN", 13));
        _column = new SpinBox { Name = "Inspect_column", MinValue = 0, MaxValue = 10, Step = 1, Value = 5, CustomMinimumSize = new Vector2(110, 33) };
        _frameControls.AddChild(_column);
        _selector = new OptionButton { Name = "Inspect_selector", Position = new Vector2(28, 115), Size = new Vector2(600, 33), Visible = false };
        if (_library is not null)
            foreach (var sprite in _library.Sprites) _selector.AddItem($"{sprite.ArchiveName} / {sprite.ObjectName}");
        root.AddChild(_selector);
        _canvas = new OriginalAssetCanvas(_library) { Name = "Inspect_canvas", Position = new Vector2(0, 154), TextureFilter = CanvasItem.TextureFilterEnum.Nearest };
        root.AddChild(_canvas);
        _row.ValueChanged += value => { if (!_updatingFrameControls) { _canvas.Row = (int)value; _canvas.QueueRedraw(); } };
        _column.ValueChanged += value => { if (!_updatingFrameControls) { _canvas.Column = (int)value; _canvas.QueueRedraw(); } };
        _selector.ItemSelected += id => { SaveFrame(); _canvas.Selected = (int)id; UpdateFrameBounds(); };
        var footer = MakeLabel("Source: fowlmouth/cosmic-rift-assets · originals unchanged · LVL physics and native banking/blit remain pending", 12);
        footer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        footer.OffsetLeft = 28; footer.OffsetTop = -30; footer.OffsetBottom = -8;
        root.AddChild(footer);
        ShowPage(_page);
    }

    public void ShowPage(string page)
    {
        if (page is not ("ships" or "sprites" or "map")) throw new ArgumentException("Unknown original-content page.", nameof(page));
        if (_canvas is not null) SaveFrame();
        _page = page;
        if (_canvas is null) return;
        _canvas.Page = page;
        _frameControls.Visible = page != "map";
        _selector.Visible = page == "sprites" && _library is not null;
        UpdateFrameBounds();
    }

    private void SaveFrame()
    {
        if (_page != "map") _frames[FrameKey] = (_canvas.Row, _canvas.Column);
    }

    private void UpdateFrameBounds()
    {
        // Overview navigation must not clamp hidden controls or destroy a frame selection.
        if (_page == "map") { _canvas.QueueRedraw(); return; }
        var sprite = _library is { Sprites.Count: > 0 } ? _library.Sprites[_canvas.Selected] : null;
        var frame = _frames.GetValueOrDefault(FrameKey, _page == "ships" ? (16, 5) : (0, 0));
        _updatingFrameControls = true;
        try
        {
            _row.MaxValue = _page == "ships" ? 63 : (sprite?.RowCount ?? 1) - 1;
            _column.MaxValue = _page == "ships" ? 10 : (sprite?.ColumnCount ?? 1) - 1;
            _row.Value = frame.Item1;
            _column.Value = frame.Item2;
            _canvas.Row = (int)_row.Value;
            _canvas.Column = (int)_column.Value;
        }
        finally { _updatingFrameControls = false; }
        _canvas.QueueRedraw();
    }

    private static Label MakeLabel(string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("d2dce1"));
        return label;
    }
}

public partial class OriginalAssetCanvas : Node2D
{
    private readonly OriginalSpriteLibrary? _library;
    public string Page { get; set; } = "ships";
    public int Row { get; set; } = 16;
    public int Column { get; set; } = 5;
    public int Selected { get; set; }
    public OriginalAssetCanvas(OriginalSpriteLibrary? library) => _library = library;

    public override void _Draw()
    {
        if (_library is null)
        {
            Text(new Vector2(28, 40), "Original content has not been staged for this build.", 21);
            Text(new Vector2(28, 74), "Run scripts/original-assets/stage_original_assets.py, then import/build the client.", 15);
            return;
        }
        if (Page == "ships") DrawHulls();
        else if (Page == "sprites") DrawSprite();
        else DrawMap();
    }

    private void DrawHulls()
    {
        var slot = new Vector2((GetViewportRect().Size.X - 56) / 4, 170);
        for (var i = 0; i < _library!.Hulls.Count; i++)
        {
            var hull = _library.Hulls[i];
            var sprite = _library.Find(hull.AssetKey);
            var origin = new Vector2(28, 0) + new Vector2(i % 4 * slot.X, i / 4 * slot.Y);
            DrawRect(new Rect2(origin, slot - new Vector2(12, 10)), new Color("101923"));
            var center = origin + new Vector2(slot.X / 2, 64);
            var size = new Vector2(sprite.Width, sprite.Height) * 2;
            DrawTextureRectRegion(_library.Texture(sprite), new Rect2(center - size / 2, size), _library.Frame(sprite, Row, Column));
            Text(origin + new Vector2(14, 129), hull.Name, 20);
            Text(origin + new Vector2(14, 149), $"VEH {hull.VehicleId} · {sprite.Width}×{sprite.Height} · 64×11 frames", 12);
        }
    }

    private void DrawSprite()
    {
        var sprite = _library!.Sprites[Selected];
        Text(new Vector2(28, 43), sprite.ObjectName, 25);
        Text(new Vector2(28, 73), $"{sprite.ArchiveName}.blo", 17);
        Text(new Vector2(28, 110), $"Cell {sprite.Width} × {sprite.Height} pixels", 15);
        Text(new Vector2(28, 136), $"{sprite.RowCount} rows × {sprite.ColumnCount} columns · {sprite.FrameCount} frames", 15);
        Text(new Vector2(28, 171), $"Viewing row {Row}, column {Column}", 17);
        Text(new Vector2(28, 208), $"Blit mode {sprite.BlitMode} · shadow colors {sprite.ShadowCount}", 15);
        Text(new Vector2(28, 244), "Original palette RGB; transparent index 0.", 15);
        Text(new Vector2(28, 270), "Native lighting/shadow/HSV rules are pending.", 15);
        Text(new Vector2(28, 314), "SHA-256 source", 13);
        Text(new Vector2(28, 338), sprite.SourceSha256[..32], 12);
        Text(new Vector2(28, 358), sprite.SourceSha256[32..], 12);
        var scale = Math.Min(4f, Math.Min(650f / sprite.Width, 430f / sprite.Height));
        var size = new Vector2(sprite.Width, sprite.Height) * scale;
        var center = new Vector2(902, 235);
        DrawRect(new Rect2(center - size / 2 - Vector2.One * 8, size + Vector2.One * 16), new Color("101923"));
        DrawTextureRectRegion(_library.Texture(sprite), new Rect2(center - size / 2, size), _library.Frame(sprite, Row, Column));
    }

    private void DrawMap()
    {
        var map = _library!.Map;
        var size = new Vector2(map.Width, map.Height);
        size *= Math.Min(880f / size.X, 510f / size.Y);
        DrawTextureRect(_library.MapTexture(), new Rect2(new Vector2(386, 0), size), false);
        Text(new Vector2(28, 43), "ROGUE TRADER", 25);
        Text(new Vector2(28, 80), "Original Rogue.bmp map view", 17);
        Text(new Vector2(28, 116), $"{map.Width} × {map.Height} pixels", 17);
        Text(new Vector2(28, 161), "This is the canonical overview.", 15);
        Text(new Vector2(28, 189), "Terrain/collision uses separate LVL data.", 15);
        Text(new Vector2(28, 216), "Map traversal is not implemented here.", 15);
        Text(new Vector2(28, 273), "Source SHA-256", 13);
        Text(new Vector2(28, 299), map.SourceSha256[..32], 12);
        Text(new Vector2(28, 319), map.SourceSha256[32..], 12);
    }

    private void Text(Vector2 position, string text, int size) =>
        DrawString(ThemeDB.FallbackFont, position, text, HorizontalAlignment.Left, -1, size, new Color("cbd7de"));
}
