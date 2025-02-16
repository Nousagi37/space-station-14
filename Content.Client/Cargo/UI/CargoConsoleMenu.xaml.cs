using System;
using System.Collections.Generic;
using Content.Client.Stylesheets;
using Content.Shared.Cargo;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Cargo.UI
{
    [GenerateTypedNameReferences]
    public partial class CargoConsoleMenu : DefaultWindow
    {
        public CargoConsoleBoundUserInterface Owner { get; private set; }

        public event Action<ButtonEventArgs>? OnItemSelected;
        public event Action<ButtonEventArgs>? OnOrderApproved;
        public event Action<ButtonEventArgs>? OnOrderCanceled;

        private readonly List<string> _categoryStrings = new();
        private string? _category;

        public CargoConsoleMenu(CargoConsoleBoundUserInterface owner)
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
            Owner = owner;

            Title = Loc.GetString(Owner.RequestOnly
                                      ? "cargo-console-menu-request-only-title"
                                      : "cargo-console-menu-title");

            CallShuttleButton.OnPressed += OnCallShuttleButtonPressed;
            SearchBar.OnTextChanged += OnSearchBarTextChanged;
            Categories.OnItemSelected += OnCategoryItemSelected;
        }

        private void OnCallShuttleButtonPressed(ButtonEventArgs args)
        {
        }

        private void OnCategoryItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            SetCategoryText(args.Id);
            PopulateProducts();
        }

        private void OnSearchBarTextChanged(LineEdit.LineEditEventArgs args)
        {
            PopulateProducts();
        }

        private void SetCategoryText(int id)
        {
            _category = id == 0 ? null : _categoryStrings[id];
            Categories.SelectId(id);
        }

        /// <summary>
        ///     Populates the list of products that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateProducts()
        {
            Products.RemoveAllChildren();

            if (Owner.Market == null)
            {
                return;
            }

            var search = SearchBar.Text.Trim().ToLowerInvariant();
            foreach (var prototype in Owner.Market.Products)
            {
                // if no search or category
                // else if search
                // else if category and not search
                if (search.Length == 0 && _category == null ||
                    search.Length != 0 && prototype.Name.ToLowerInvariant().Contains(search) ||
                    search.Length == 0 && _category != null && prototype.Category.Equals(_category))
                {
                    var button = new CargoProductRow
                    {
                        Product = prototype,
                        ProductName = { Text = prototype.Name },
                        PointCost = { Text = prototype.PointCost.ToString() },
                        Icon = { Texture = prototype.Icon.Frame0() },
                    };
                    button.MainButton.OnPressed += args =>
                    {
                        OnItemSelected?.Invoke(args);
                    };
                    Products.AddChild(button);
                }
            }
        }

        /// <summary>
        ///     Populates the list of products that will actually be shown, using the current filters.
        /// </summary>
        public void PopulateCategories()
        {
            _categoryStrings.Clear();
            Categories.Clear();

            if (Owner.Market == null)
            {
                return;
            }

            _categoryStrings.Add(Loc.GetString("cargo-console-menu-populate-categories-all-text"));

            foreach (var prototype in Owner.Market.Products)
            {
                if (!_categoryStrings.Contains(prototype.Category))
                {
                    _categoryStrings.Add(Loc.GetString(prototype.Category));
                }
            }
            _categoryStrings.Sort();
            foreach (var str in _categoryStrings)
            {
                Categories.AddItem(str);
            }
        }

        /// <summary>
        ///     Populates the list of orders and requests.
        /// </summary>
        public void PopulateOrders()
        {
            Orders.RemoveAllChildren();
            Requests.RemoveAllChildren();

            if (Owner.Orders == null || Owner.Market == null)
            {
                return;
            }

            foreach (var order in Owner.Orders.Orders)
            {
                var productName = Owner.Market.GetProduct(order.ProductId)?.Name;

                if (productName == null)
                {
                    DebugTools.Assert(false);
                    Logger.ErrorS("cargo", $"Unable to find product name for {order.ProductId}");
                    continue;
                }

                var row = new CargoOrderRow
                {
                    Order = order,
                    Icon = { Texture = Owner.Market.GetProduct(order.ProductId)?.Icon.Frame0() },
                    ProductName =
                    {
                        Text = Loc.GetString(
                            "cargo-console-menu-populate-orders-cargo-order-row-product-name-text",
                            ("productName", productName),
                            ("orderAmount", order.Amount),
                            ("orderRequester", order.Requester))
                    },
                    Description = {Text = Loc.GetString("cargo-console-menu-order-reason-description",
                                                        ("reason", order.Reason))}
                };
                row.Cancel.OnPressed += (args) => { OnOrderCanceled?.Invoke(args); };
                if (order.Approved)
                {
                    row.Approve.Visible = false;
                    row.Cancel.Visible = false;
                    Orders.AddChild(row);
                }
                else
                {
                    if (Owner.RequestOnly)
                        row.Approve.Visible = false;
                    else
                        row.Approve.OnPressed += (args) => { OnOrderApproved?.Invoke(args); };
                    Requests.AddChild(row);
                }
            }
        }

        public void Populate()
        {
            PopulateProducts();
            PopulateCategories();
            PopulateOrders();
        }

        public void UpdateCargoCapacity()
        {
            ShuttleCapacityLabel.Text = $"{Owner.ShuttleCapacity.CurrentCapacity}/{Owner.ShuttleCapacity.MaxCapacity}";
        }

        public void UpdateBankData()
        {
            AccountNameLabel.Text = Owner.BankName;
            PointsLabel.Text = Owner.BankBalance.ToString();
        }

        /// <summary>
        ///     Show/Hide Call Shuttle button and Approve buttons
        /// </summary>
        public void UpdateRequestOnly()
        {
            CallShuttleButton.Visible = !Owner.RequestOnly;
            foreach (CargoOrderRow row in Requests.Children)
            {
                row.Approve.Visible = !Owner.RequestOnly;
            }
        }
    }
}
