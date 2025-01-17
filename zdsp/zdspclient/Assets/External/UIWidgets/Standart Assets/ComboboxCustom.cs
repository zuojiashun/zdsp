﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

namespace UIWidgets
{

	/// <summary>
	/// Base class for custom combobox.
	/// </summary>
	public class ComboboxCustom<TListViewCustom,TComponent,TItem> : MonoBehaviour, ISubmitHandler
			where TListViewCustom : ListViewCustom<TComponent,TItem>
			where TComponent : ListViewItem
	{
		/// <summary>
		/// Custom Combobox event.
		/// </summary>
		public class ComboboxCustomEvent : UnityEvent<int,TItem>
		{
			
		}

		[SerializeField]
		TListViewCustom listView;
		
		/// <summary>
		/// Gets or sets the ListView.
		/// </summary>
		/// <value>ListView component.</value>
		public TListViewCustom ListView {
			get {
				return listView;
			}
			set {
				if (listView!=null)
				{
					listParent = null;

					listView.OnSelectObject.RemoveListener(SetCurrent);
					listView.OnSelectObject.RemoveListener(onSelectCallback);
					listView.OnFocusOut.RemoveListener(onFocusHideList);

					listView.onCancel.RemoveListener(OnListViewCancel);
					listView.onItemCancel.RemoveListener(OnListViewCancel);

					RemoveDeselectCallbacks();
				}
				listView = value;
				if (listView!=null)
				{
					listParent = listView.transform.parent;

					listView.OnSelectObject.AddListener(SetCurrent);
					listView.OnSelectObject.AddListener(onSelectCallback);

					listView.OnFocusOut.AddListener(onFocusHideList);

					listView.onCancel.AddListener(OnListViewCancel);
					listView.onItemCancel.AddListener(OnListViewCancel);

					AddDeselectCallbacks();
				}
			}
		}
		
		[SerializeField]
		Button toggleButton;
		
		/// <summary>
		/// Gets or sets the toggle button.
		/// </summary>
		/// <value>The toggle button.</value>
		public Button ToggleButton {
			get {
				return toggleButton;
			}
			set {
				if (toggleButton!=null)
				{
					toggleButton.onClick.RemoveListener(ToggleList);
				}
				toggleButton = value;
				if (toggleButton!=null)
				{
					toggleButton.onClick.AddListener(ToggleList);
				}
			}
		}

		[SerializeField]
		TComponent current;

		/// <summary>
		/// Gets or sets the current component.
		/// </summary>
		/// <value>The current.</value>
		public TComponent Current {
			get {
				return current;
			}
			set {
				current = value;
			}
		}
		
		/// <summary>
		/// OnSelect event.
		/// </summary>
		public ComboboxCustomEvent OnSelect = new ComboboxCustomEvent();
		UnityAction<int> onSelectCallback;

		Transform listCanvas;
		Transform listParent;

		void Awake()
		{
			Start();
		}
		
		[System.NonSerialized]
		private bool isStartedComboboxCustom;

		/// <summary>
		/// Start this instance.
		/// </summary>
		public virtual void Start()
		{
			if (isStartedComboboxCustom)
			{
				return ;
			}
			isStartedComboboxCustom = true;

			onSelectCallback = (index) => OnSelect.Invoke(index, listView.Items[index]);

			ToggleButton = toggleButton;

			ListView = listView;

			if (listView!=null)
			{
				listView.OnSelectObject.RemoveListener(SetCurrent);

				listCanvas = Utilites.FindCanvas(listParent);

				listView.gameObject.SetActive(true);
				listView.Start();
				if ((listView.SelectedIndex==-1) && (listView.Items.Count > 0))
				{
					listView.SelectedIndex = 0;
				}
				if (listView.SelectedIndex!=-1)
				{
					UpdateCurrent();
				}
				listView.gameObject.SetActive(false);

				listView.OnSelectObject.AddListener(SetCurrent);
			}
		}

		/// <summary>
		/// Clear listview and selected item.
		/// </summary>
		public virtual void Clear()
		{
			listView.Clear();
			UpdateCurrent();
		}

		/// <summary>
		/// Toggles the list visibility.
		/// </summary>
		public void ToggleList()
		{
			if (listView==null)
			{
				return ;
			}

			if (listView.gameObject.activeSelf)
			{
				HideList();
			}
			else
			{
				ShowList();
			}
		}

		bool isOpeningList;

		/// <summary>
		/// Shows the list.
		/// </summary>
		public void ShowList()
		{
			if (listView==null)
			{
				return ;
			}
			if (listCanvas!=null)
			{
				listParent = listView.transform.parent;
				listView.transform.SetParent(listCanvas);
			}
			listView.gameObject.SetActive(true);

			if (listView.Layout!=null)
			{
				listView.Layout.UpdateLayout();
			}

			if (listView.SelectComponent())
			{
				SetChildDeselectListener(EventSystem.current.currentSelectedGameObject);
			}
			else
			{
				EventSystem.current.SetSelectedGameObject(listView.gameObject);
			}
		}

		/// <summary>
		/// Hides the list.
		/// </summary>
		public void HideList()
		{
			if (listView==null)
			{
				return ;
			}

			listView.gameObject.SetActive(false);
			if (listCanvas!=null)
			{
				listView.transform.SetParent(listParent);
			}
		}

		List<SelectListener> childrenDeselect = new List<SelectListener>();
		void onFocusHideList(BaseEventData eventData)
		{
			if (eventData.selectedObject==gameObject)
			{
				return ;
			}

			var ev_item = eventData as ListViewItemEventData;
			if (ev_item!=null)
			{
				if (ev_item.NewSelectedObject!=null)
				{
					SetChildDeselectListener(ev_item.NewSelectedObject);
				}
				return ;
			}

			var ev = eventData as PointerEventData;
			if (ev==null)
			{
				HideList();
				return ;
			}

			var go = ev.pointerPressRaycast.gameObject;//ev.pointerEnter
			if (go==null)
			{
				HideList();
				return ;
			}

			if (go.Equals(toggleButton.gameObject))
			{
				return ;
			}
			if (go.transform.IsChildOf(listView.transform))
			{
				SetChildDeselectListener(go);
				return ;
			}

			HideList();
		}

		void SetChildDeselectListener(GameObject child)
		{
			var deselectListener = GetDeselectListener(child);
			if (!childrenDeselect.Contains(deselectListener))
			{
				deselectListener.onDeselect.AddListener(onFocusHideList);
				childrenDeselect.Add(deselectListener);
			}
		}

		SelectListener GetDeselectListener(GameObject go)
		{
			return go.GetComponent<SelectListener>() ?? go.AddComponent<SelectListener>();
		}

		void AddDeselectCallbacks()
		{
			if (listView.ScrollRect!=null)
			{
				var scrollbar = listView.ScrollRect.verticalScrollbar.gameObject;
				var deselectListener = GetDeselectListener(scrollbar);

				deselectListener.onDeselect.AddListener(onFocusHideList);
				childrenDeselect.Add(deselectListener);
			}
		}

		void RemoveDeselectCallbacks()
		{
			childrenDeselect.ForEach(x => {
				if (x!=null)
				{
					x.onDeselect.RemoveListener(onFocusHideList);
				}
			});
			childrenDeselect.Clear();
		}

		void SetCurrent(int index)
		{
			UpdateCurrent();

			if ((EventSystem.current!=null) && (!EventSystem.current.alreadySelecting))
			{
				EventSystem.current.SetSelectedGameObject(gameObject);
			}
		}

		void OnListViewCancel()
		{
			HideList();
		}

		/// <summary>
		/// Raises the submit event.
		/// </summary>
		/// <param name="eventData">Event data.</param>
		void ISubmitHandler.OnSubmit(BaseEventData eventData)
		{
			ShowList();
		}

		/// <summary>
		/// Updates the current component.
		/// </summary>
		protected virtual void UpdateCurrent()
		{
			HideList();
		}
	}
}