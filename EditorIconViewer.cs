using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class EditorIconViewer : EditorWindow
{
	public class IconData
	{
		public int index;
		public GUIStyle style;
		public Texture2D texture;
	}
	
	public class IconGroup
	{
		public string name;
		public IconData[] iconData;
		public float iconWidthThreshold;
		public float maxWidth;
	}
	
	// Icons are categorized by their height, into buckets defined by
	// the two arrays below. The number of thresholds should always exceed
	// the number of group names by one.
	public List<IconGroup> iconGroups;
	public static float[] kIconThresholds = {  0,     9,       25,       35,     100,        99999 };
	public static string[] kIconGroupNames = {  "Mini", "Small", "Medium", "Large", "X-Large" };
	
	#region Blacklisted Items
	// Names of known style states that have a texture present in the 'background' field but
	// whose icons show up as empty images when renderered.
	protected static bool kHideBlacklistedIcons = true;
	protected static HashSet<string> kIconBlacklist = new HashSet<string>()
	{
		"PlayerSettingsPlatform",
		"PreferencesSection",
		"ProfilerPaneLeftBackground",
		"flow var 0",
		"flow var 0 on",
		"flow var 1",
		"flow var 1 on",
		"flow var 2",
		"flow var 2 on",
		"flow var 3",
		"flow var 3 on",
		"flow var 4",
		"flow var 4 on",
		"flow var 5",
		"flow var 5 on",
		"flow var 6",
		"flow var 6 on",
	};
	#endregion
	
	public static float kSidePanelMinWidth = 150;
	public static float kSidePanelMaxWidth = 250;
	public static float kScrollbarWidth = 15;
	public static float kSelectionGridPadding = 10;
	
	public static string kUsageString = 
		"All of the icons presented in this collection are easily accessible when writing a custom editor script, for both Inspectors and Editor Windows. " +
		"In the OnEnable method of your editor, obtain a copy of the editor's skin with the following:\n\n"+
		"GUISkin _editorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);\n\n"+
		"Textures shown in this tool can be retrieved by using their style names, shown at the top of the left-hand panel when you select an icon from the grid. For example:\n\n"+
		"GUILayout.Button(_editorSkin.GetStyle(\"MeTransPlayhead\").normal.background);\n\n"+
		"Or you can simply use the style itself when rendering a control:\n\n"+
		"GUILayout.Button(\"\", _editorSkin.GetStyle(\"MeTransPlayhead\"));\n\n"+
		"If additional style states are available (such as Focused, Hover, or Active), they will appear in the panel when selected.";
	
	protected GUISkin _editorSkin;
	protected IconData _selectedIcon;
	
	[MenuItem("Tools/Editor Icons")]
    static void Init()
    {
        EditorIconViewer window = (EditorIconViewer)GetWindow(typeof(EditorIconViewer), false, "Editor Icon Viewer");
        window.position = new Rect(150, 150, 700, 400);
    }
	
	void OnEnable()
	{
		iconGroups = new List<IconGroup>();
		_editorSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
		
		for (int i = 0; i < kIconGroupNames.Length; ++i)
		{
			IconGroup group = new IconGroup();
			group.name = kIconGroupNames[i];
			
			float minHeight = kIconThresholds[i];
			float maxHeight = kIconThresholds[i + 1];
			
			group.iconData = _editorSkin.customStyles
			.Where(style => 
				{
					if (style.normal.background == null)
						return false;
					if (style.normal.background.height <= minHeight || style.normal.background.height > maxHeight)
						return false;
					if (kHideBlacklistedIcons && kIconBlacklist.Contains(style.name))
						return false;
					
					return true;
				})
			.Select(style => new IconData
			{
				style = style,
				texture = style.normal.background
			}).OrderBy(iconData => iconData.texture.height).ToArray();
			
			float maxWidth = 0;
			foreach (IconData icon in group.iconData)
				maxWidth = (icon.texture.width > maxWidth) ? icon.texture.width : maxWidth;
			group.maxWidth = maxWidth;
			
			iconGroups.Add(group);
		}
		
		SetSelectedIcon(null);
	}
	
	protected Vector2 _scrollPos = Vector2.zero;
	void OnGUI()
	{
		float sidePanelWidth = CalculateSidePanelWidth();
		GUILayout.BeginArea(new Rect(0, 0, sidePanelWidth, position.height), GUI.skin.box);
		DrawIconDisplay(_selectedIcon);
		GUILayout.EndArea();
		
		GUI.BeginGroup(new Rect(sidePanelWidth, 0, position.width - sidePanelWidth, position.height));
		_scrollPos = GUILayout.BeginScrollView(_scrollPos, true, true, GUILayout.MaxWidth(position.width - sidePanelWidth));
		
		for (int i = 0; i < iconGroups.Count; ++i)
		{
			IconGroup group = iconGroups[i];
			EditorGUILayout.LabelField(group.name);
			DrawIconSelectionGrid(group.iconData, group.maxWidth);
			
			GUILayout.Space(15);	
		}
				
		GUILayout.EndScrollView();
		GUI.EndGroup();
	}
	
	protected float CalculateSidePanelWidth()
	{
		return Mathf.Clamp(position.width * 0.21f, kSidePanelMinWidth, kSidePanelMaxWidth);
	}
	
	protected float _drawScale = 1.0f;
	protected void DrawIconDisplay(IconData icon)
	{
		if (icon == null)
		{
			DrawCenteredMessage("No icon selected");
			GUILayout.FlexibleSpace();
			DrawHelpIcon();
			return;
		}
		
		EditorGUILayout.BeginVertical();
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(icon.style.name, EditorStyles.whiteLargeLabel);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label("Normal");
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		
		float iconOffset = 45;
		float iconWidth = icon.texture.width * _drawScale;
		float iconHeight = icon.texture.height * _drawScale;
		float sidePanelWidth = CalculateSidePanelWidth();
		GUI.DrawTexture(new Rect((sidePanelWidth - iconWidth) * 0.5f, iconOffset, iconWidth, iconHeight), icon.texture, ScaleMode.StretchToFill);
		GUILayout.Space(iconHeight + 10);
		
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Toggle(_drawScale == 1.0f, "1x", EditorStyles.miniButtonLeft))
		{
			_drawScale = 1.0f;
		}
		if (GUILayout.Toggle(_drawScale == 1.5f, "1.5x", EditorStyles.miniButtonMid))
		{
			_drawScale = 1.5f;
		}
		if (GUILayout.Toggle(_drawScale == 2.0f, "2x", EditorStyles.miniButtonRight))	
		{
			_drawScale = 2.0f;
		}
		EditorGUILayout.EndHorizontal();
		
		GUILayout.Space(10);
		
		DrawIconStyleState(icon.style.active, "Active");
		DrawIconStyleState(icon.style.hover, "Hover");
		DrawIconStyleState(icon.style.focused, "Focused");
		
		GUILayout.Space(10);
		
		EditorGUILayout.LabelField(string.Format("Width:      {0}px", icon.texture.width));
		EditorGUILayout.LabelField(string.Format("Height:    {0}px", icon.texture.height));
		
		GUILayout.FlexibleSpace();
		DrawHelpIcon();
		
		EditorGUILayout.EndVertical();
	}
	
	protected void DrawIconStyleState(GUIStyleState state, string label)
	{
		if (state == null || state.background == null)
			return;
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Label(label);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
		
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		GUILayout.Box(state.background);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}
	
	protected void SetSelectedIcon(IconData icon)
	{
		_selectedIcon = icon;
		_drawScale = 1.0f;
	}
	
	protected void DrawIconSelectionGrid(IconData[] icons, float maxIconWidth)
	{
		float sidePanelWidth = CalculateSidePanelWidth();
		int xCount = Mathf.FloorToInt((position.width - sidePanelWidth - kScrollbarWidth) / (maxIconWidth + kSelectionGridPadding));
		int selected = GUILayout.SelectionGrid(-1, icons.Select(data => data.texture).ToArray(), xCount, GUI.skin.box);
		
		if (selected > -1)
		{
			SetSelectedIcon(icons[selected]);
		}
	}
	
	protected void DrawCenteredMessage(string msg)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		EditorGUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
		GUILayout.Label(msg);
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}
	
	protected void DrawHelpIcon()
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("", _editorSkin.GetStyle("CN EntryInfo")))
		{
			EditorUtility.DisplayDialog("Editor Icon Viewer", kUsageString, "Ok");
		}
		EditorGUILayout.EndHorizontal();
	}
}
