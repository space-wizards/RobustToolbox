using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.ViewVariables.Editors;
using Robust.Client.ViewVariables.Instances;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables.Traits
{
    internal class ViewVariablesTraitEnumerable : ViewVariablesTrait
    {
        private const int ElementsPerPage = 25;
        private readonly List<object?> _cache = new();
        private int _page;
        private IEnumerator? _enumerator;
        private bool _ended;
        private bool _networked;

        private Button _leftButton = default!;
        private Button _rightButton = default!;
        private LineEdit _pageLabel = default!;
        private HBoxContainer _controlsHBox = default!;
        private VBoxContainer _elementsVBox = default!;

        private int HighestKnownPage => Math.Max(0, ((_cache.Count + ElementsPerPage - 1) / ElementsPerPage) - 1);

        private SemaphoreSlim? _networkSemaphore;

        public override void Initialize(ViewVariablesInstanceObject instance)
        {
            base.Initialize(instance);
            if (instance.Object == null)
            {
                DebugTools.Assert(instance.Session != null);
                _networked = true;
                _networkSemaphore = new SemaphoreSlim(1, 1);
            }
            else
            {
                var enumerable = (IEnumerable) instance.Object;
                _enumerator = enumerable.GetEnumerator();
            }

            var outerVBox = new VBoxContainer();
            _controlsHBox = new HBoxContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
            };

            {
                // Page navigational controls.
                _leftButton = new Button {Text = "<<", Disabled = true};
                _pageLabel = new LineEdit {Text = "0", CustomMinimumSize = (60, 0)};
                _rightButton = new Button {Text = ">>"};

                _leftButton.OnPressed += _leftButtonPressed;
                _pageLabel.OnTextEntered += _lineEditTextEntered;
                _rightButton.OnPressed += _rightButtonPressed;

                _controlsHBox.AddChild(_leftButton);
                _controlsHBox.AddChild(_pageLabel);
                _controlsHBox.AddChild(_rightButton);
            }

            outerVBox.AddChild(_controlsHBox);

            _elementsVBox = new VBoxContainer();
            outerVBox.AddChild(_elementsVBox);

            instance.AddTab("IEnumerable", outerVBox);
        }

        public override async void Refresh()
        {
            _cache.Clear();
            _ended = false;
            if (_networked)
            {
                await _cacheTo(1, true);
            }
            else
            {
                var enumerable = (IEnumerable) Instance.Object!;
                _enumerator = enumerable.GetEnumerator();
            }

            await _moveToPage(_page);
        }

        private async void _lineEditTextEntered(LineEdit.LineEditEventArgs obj)
        {
            await _moveToPage(int.Parse(obj.Text, CultureInfo.InvariantCulture));
        }

        private async void _rightButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            await _moveToPage(_page + 1);
        }

        private async void _leftButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            await _moveToPage(_page - 1);
        }

        private async Task _moveToPage(int page)
        {
            // TODO: Network overhead optimization potential:
            // Right now, (in NETWORK mode) if I request page 5, it has to cache all 5 pages,
            // now the server obviously (enumerator and all that) has to TOO, but whatever.
            // The waste is that all pages are also SENT, even though we only really care about the fifth at the moment.
            // Because the cache can't have holes (and also the network system is too simplistic at the moment,
            // if you do do a by-page pull and you're way too far along,
            // you'll just get 0 elements which doesn't tell you where it ended but that's kinda necessary.
            if (page < 0)
            {
                page = 0;
            }

            if (page > HighestKnownPage || (!_ended && page == HighestKnownPage))
            {
                if (_ended)
                {
                    // The requested page is higher than the highest page we have (and we know this because the enumerator ended).
                    page = HighestKnownPage;
                }
                else
                {
                    // The page is higher than the highest page we have, but the enumerator hasn't ended yet so that might be valid.
                    // Gotta get more data.
                    await _cacheTo((page + 1) * ElementsPerPage);

                    if (page > HighestKnownPage)
                    {
                        // We tried, but the enumerator ended before we reached our goal.
                        // Oh well.
                        DebugTools.Assert(_ended);
                        page = HighestKnownPage;
                    }
                }
            }

            _elementsVBox.DisposeAllChildren();

            for (var i = page * ElementsPerPage; i < ElementsPerPage * (page + 1) && i < _cache.Count; i++)
            {
                var element = _cache[i];
                VVPropEditor editor;
                if (element == null)
                {
                    editor = new VVPropEditorDummy();
                }
                else
                {
                    var type = element.GetType();
                    editor = Instance.ViewVariablesManager.PropertyFor(type);
                }

                var control = editor.Initialize(element, true);
                if (_networked)
                {
                    var selectorChain = new object[] {new ViewVariablesEnumerableIndexSelector(i)};
                    editor.WireNetworkSelector(Instance.Session!.SessionId, selectorChain);
                }

                _elementsVBox.AddChild(control);
            }

            _page = page;

            _updateControls();
        }

        private void _updateControls()
        {
            if (_ended && HighestKnownPage == 0)
            {
                _controlsHBox.Visible = false;
                return;
            }
            else
            {
                _controlsHBox.Visible = true;
            }


            _leftButton.Disabled = _page == 0;
            _pageLabel.Text = $"{_page + 1}";
            _rightButton.Disabled = _page == HighestKnownPage && _ended;
        }

        private async Task _cacheTo(int index, bool netRefresh=false)
        {
            DebugTools.Assert(_networked || !netRefresh);
            if (index < _cache.Count)
            {
                // This check is probably redundant, oh well.
                return;
            }
            if (_networked)
            {
                await _networkSemaphore!.WaitAsync();

                try
                {
                    // I believe it may theoretically be possible to hit this, so...
                    if (index < _cache.Count)
                    {
                        return;
                    }

                    // Would it maybe be a good idea to send this in chunks?
                    // Too lazy to code that right now.
                    // Oh well..
                    var blob = await Instance.ViewVariablesManager.RequestData<ViewVariablesBlobEnumerable>(Instance.Session!,
                        new ViewVariablesRequestEnumerable(_cache.Count, index, netRefresh));

                    _cache.AddRange(blob.Objects);

                    if (_cache.Count < index)
                    {
                        _ended = true;
                    }
                }
                finally
                {
                    _networkSemaphore.Release();
                }
            }
            else
            {
                DebugTools.AssertNotNull(_enumerator);
                while (_cache.Count < index)
                {
                    if (!_enumerator!.MoveNext())
                    {
                        _ended = true;
                        break;
                    }

                    _cache.Add(_enumerator!.Current);
                }
            }
        }
    }
}
