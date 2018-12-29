using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor;
using FlaxEditor.Content;
using FlaxEditor.GUI;
using FlaxEditor.GUI.Drag;
using FlaxEngine;
using FlaxEngine.GUI;
using static FlaxEditor.Viewport.MainEditorGizmoViewport;

namespace ImplicitSurfacePlugin
{
	public class Setup : EditorPlugin
	{
		public override PluginDescription Description => new PluginDescription()
		{
			Name = "Toolbox Script Drag n Drop Demo Plugin",
			Author = "Stefnotch",
			RepositoryUrl = "", //TODO: repository url
			Version = new Version(1, 0),
			SupportedPlatforms = new[]
			{
				PlatformType.Windows
			},
			IsAlpha = false,
			IsBeta = false
		};

		private Tab _tab = null;
		private ToolboxScriptDragHandler _customDragHandler = null;

		public override void InitializeEditor()
		{
			base.InitializeEditor();

			var toolboxTabs = Editor.Windows.ToolboxWin.TabsControl;

			var defaultToolbox = toolboxTabs.Children.OfType<Tab>().First();

			var actorGroups = defaultToolbox.Children.OfType<Tabs>().First();

			_tab = actorGroups.AddTab(new Tab("Demo"));
			var panel = new Panel(ScrollBars.Both)
			{
				DockStyle = DockStyle.Fill,
				Parent = _tab
			};
			ContainerControl groupImplicitSurface = new Tree(false)
			{
				DockStyle = DockStyle.Top,
				IsScrollable = true,
				Parent = panel
			};

			var editorViewport = Editor.Windows.EditWin.Viewport;
			_customDragHandler = new ToolboxScriptDragHandler(_ => true);
			editorViewport.DragHandlers.Add(_customDragHandler);

			var scriptItem1 = Editor.Instance.ContentDatabase.FindScriptWitScriptName("DemoScript");

			var i1 = groupImplicitSurface.AddChild(new Item("DemoScript", _customDragHandler.ToDragData(scriptItem1)));

			// Hmm, one annoying issue still remains
			toolboxTabs.PerformLayout(true);
			defaultToolbox.PerformLayout(true);
			actorGroups.PerformLayout(true);
			_tab.PerformLayout(true);
			panel.PerformLayout(true);
			groupImplicitSurface.PerformLayout(true);
			foreach (var child in groupImplicitSurface.Children)
			{
				child.PerformLayout(true);
			}
		}

		public sealed class ToolboxScriptDragHandler : ToolboxScriptDragHandler<DragEventArgs>
		{
			public ToolboxScriptDragHandler(Func<ScriptItem, bool> validateFunction) : base(validateFunction)
			{
			}

			private Vector3 PostProcessSpawnedActorLocation(Actor actor, ref Vector3 hitLocation)
			{
				BoundingBox box;
				Editor.GetActorEditorBox(actor, out box);
				var editorInstance = FlaxEditor.Editor.Instance;
				if (editorInstance == null) return hitLocation;

				var editorViewport = editorInstance.Windows.EditWin.Viewport;
				if (editorViewport == null) return hitLocation;
				// Place the object
				var location = hitLocation - (box.Size.Length * 0.5f) * editorViewport.ViewDirection;

				// Apply grid snapping if enabled
				if (editorViewport.UseSnapping || editorViewport.TransformGizmo.TranslationSnapEnable)
				{
					float snapValue = editorViewport.TransformGizmo.TranslationSnapValue;
					location = new Vector3(
						(int)(location.X / snapValue) * snapValue,
						(int)(location.Y / snapValue) * snapValue,
						(int)(location.Z / snapValue) * snapValue);
				}

				return location;
			}

			private List<Type> GetScriptTypes(List<ScriptItem> items)
			{
				var list = new List<Type>(items.Count);
				for (int i = 0; i < items.Count; i++)
				{
					var item = items[i];
					var scriptName = item.ScriptName;
					var scriptType = FlaxEditor.Scripting.ScriptsBuilder.FindScript(scriptName);
					if (scriptType == null)
					{
						Editor.LogWarning("Invalid script type " + scriptName);
					}
					else
					{
						list.Add(scriptType);
					}
				}
				return list;
			}

			/// <summary>
			/// Handler drag drop event.
			/// </summary>
			/// <param name="dragEventArgs">The drag event arguments.</param>
			/// <param name="item">The item.</param>
			public override void DragDrop(DragEventArgs dragEventArgs, IEnumerable<ScriptItem> item)
			{
				if (dragEventArgs is DragDropEventArgs dragDropEventArgs)
				{
					Vector3 hitLocation = dragDropEventArgs.HitLocation;
					var scriptItems = item.ToList();
					var scriptTypes = GetScriptTypes(scriptItems);
					for (int i = 0; i < scriptItems.Count; i++)
					{
						var script = (Script)FlaxEngine.Object.New(scriptTypes[i]);
						var actor = EmptyActor.New();
						actor.Name = scriptItems[i].ScriptName; // TODO: Or ShortName?
						actor.AddScript(script);
						actor.Position = PostProcessSpawnedActorLocation(actor, ref hitLocation);
						Editor.Instance.SceneEditing.Spawn(actor);
					}
				}
				else
				{
					Debug.Log("No drag n drop. Unknown DragEventArgs");
				}
			}
		}

		public class ToolboxScriptDragHandler<U> : DragHelper<ScriptItem, U> where U : DragEventArgs
		{
			/// <summary>
			/// The default prefix for drag data used for <see cref="ContentItem"/>.
			/// </summary>
			public const string DragPrefix = DragItems<DragEventArgs>.DragPrefix;

			/// <summary>
			/// Creates a new DragHelper
			/// </summary>
			/// <param name="validateFunction">The validation function</param>
			public ToolboxScriptDragHandler(Func<ScriptItem, bool> validateFunction)
			: base(validateFunction)
			{
			}

			/// <inheritdoc/>
			public override DragData ToDragData(ScriptItem item) => GetDragData(item);

			/// <inheritdoc/>
			public override DragData ToDragData(IEnumerable<ScriptItem> items) => GetDragData(items);

			/// <summary>
			/// Gets the drag data.
			/// </summary>
			/// <param name="item">The item.</param>
			/// <returns>The data.</returns>
			public static DragData GetDragData(ScriptItem item)
			{
				return DragItems<DragEventArgs>.GetDragData(item);
			}

			/// <summary>
			/// Gets the drag data.
			/// </summary>
			/// <param name="items">The items.</param>
			/// <returns>The data.</returns>
			public static DragData GetDragData(IEnumerable<ScriptItem> items)
			{
				return DragItems<DragEventArgs>.GetDragData(items);
			}

			/// <summary>
			/// Tries to parse the drag data.
			/// </summary>
			/// <param name="data">The data.</param>
			/// <returns>
			/// Gathered objects or empty IEnumerable if cannot get any valid.
			/// </returns>
			public override IEnumerable<ScriptItem> FromDragData(DragData data)
			{
				if (data is DragDataText dataText)
				{
					if (dataText.Text.StartsWith(DragPrefix))
					{
						// Remove prefix and parse spitted names
						var paths = dataText.Text.Remove(0, DragPrefix.Length).Split('\n');
						var results = new List<ScriptItem>(paths.Length);
						for (int i = 0; i < paths.Length; i++)
						{
							// Find element
							var obj = Editor.Instance.ContentDatabase.FindScript(paths[i]);

							// Check it
							if (obj != null)
								results.Add(obj);
						}

						return results.ToArray();
					}
				}
				return new ScriptItem[0];
			}
		}

		private class Item : TreeNode
		{
			private DragData _dragData;

			public Item(string text, DragData dragData = null)
			: this(text, dragData, Sprite.Invalid)
			{
			}

			public Item(string text, DragData dragData, Sprite icon)
			: base(false, icon, icon)
			{
				Text = text;
				_dragData = dragData;
				Height = 20;
				TextMargin = new Margin(-5.0f, 2.0f, 2.0f, 2.0f);
			}

			/// <inheritdoc />
			protected override void DoDragDrop()
			{
				if (_dragData != null)
					base.DoDragDrop(_dragData);
			}
		}

		public override void Deinitialize()
		{
			if (_tab != null)
			{
				_tab.Dispose();
				_tab = null;
			}
			if (_customDragHandler != null)
			{
				Editor.Windows.EditWin.Viewport.DragHandlers.Remove(_customDragHandler);
			}
			base.Deinitialize();
		}
	}
}