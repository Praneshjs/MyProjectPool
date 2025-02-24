﻿using MerchantDAL.Models;
using MerchantService.QR;
using MerchantService.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Merchant.Controls
{
    /// <summary>
    /// Interaction logic for ProductControl.xaml
    /// </summary>
    public partial class ProductControl : UserControl
    {
        private int currentPageIndex = 1;
        private int itemsPerPage = 15;
        public ProductControl()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            GetAllProductAsync(currentPageIndex);
            LoadControlTypeAsync();
            SetDefaultValue();
        }

        private void SetDefaultValue()
        {
            txtExpiryDate.SelectedDate = DateTime.Now.AddMonths(6);
            txtMfgDate.SelectedDate = DateTime.Now.AddDays(-10);
        }

        private async void LoadControlTypeAsync()
        {
            ProductMasterService fetch = new ProductMasterService();
            var masterDatas = await fetch.GetProductMasterAsync(0, null, true);
            var brandList = masterDatas.Where(s => s.CommonControl.ControlType == "Brand")
                .Select(t => new CommonDataModel() { Id = t.Id, ControlValue = t.ControlValue }).ToList();
            brandList.Add(new CommonDataModel { Id = 0, ControlValue = "Select Brand" });
            cmbBrandName.ItemsSource = brandList.OrderBy(s => s.Id);
            cmbBrandName.SelectedValue = 0;

            var productCategoryList = masterDatas.Where(s => s.CommonControl.ControlType == "Product Category")
                .Select(t => new CommonDataModel() { Id = t.Id, ControlValue = t.ControlValue }).ToList();
            productCategoryList.Add(new CommonDataModel { Id = 0, ControlValue = "Select Product Type" });
            cmbProductType.ItemsSource = productCategoryList.OrderBy(s => s.Id);
            cmbProductType.SelectedIndex = 0;
            cmbProductType.Items.Refresh();

            var productWeightType = masterDatas.Where(s => s.CommonControl.ControlType == "Weight Type")
               .Select(t => new CommonDataModel() { Id = t.Id, ControlValue = t.ControlValue }).ToList();
            cmbWeightType.ItemsSource = productWeightType.OrderBy(s => s.Id);
            cmbWeightType.SelectedIndex = 0;
        }

        private async void GetAllProductAsync(int pageIndex, DateTime? expiryDate = null, string allInfo = null, bool? isActive = null)
        {
            ProductService fetchService = new ProductService();
            var allData = await fetchService.GetProductAsync(allInfo, isActive);
            BindProductGridData(allData, pageIndex);
        }
        private void BindProductGridData(List<ProductModel> allData, int pageIndex)
        {
            int startIndex = (pageIndex * itemsPerPage) - itemsPerPage + 1;
            int endIndex = (startIndex - 1) + itemsPerPage;
            int skipCount = startIndex - 1;
            var totalPages = (int)Math.Ceiling((decimal)allData.Count() / itemsPerPage);
            UpdatePaginationInfo(pageIndex, totalPages);
            var paginationList = allData.Skip(skipCount).Take(itemsPerPage);

            lstProduct.ItemsSource = null;
            lstProduct.ItemsSource = paginationList;
        }

        private async void btnAddProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int.TryParse(cmbBrandName.SelectedValue?.ToString(), out int brandNameId);
                int.TryParse(cmbProductType.SelectedValue?.ToString(), out int productTypeId);
                int.TryParse(cmbWeightType.SelectedValue?.ToString(), out int weightTypeId);
                int.TryParse(txtQuantity.Text, out int quantity);
                decimal.TryParse(txtWeight.Text, out decimal weight);
                decimal.TryParse(txtStockPrice.Text, out decimal stockPrice);
                decimal.TryParse(txtSellingPrice.Text, out decimal sellingPrice);
                DateTime.TryParse(txtMfgDate.Text, out DateTime manufacturedDate);
                DateTime.TryParse(txtExpiryDate.Text, out DateTime expiryDate);

                StringBuilder validationMsg = new StringBuilder();
                if (quantity == 0) quantity = 1;
                if (brandNameId == 0) validationMsg.AppendLine("Select a brand name");
                if (productTypeId == 0) validationMsg.AppendLine("Select a product type");
                if (manufacturedDate == DateTime.MinValue) validationMsg.AppendLine("Invalid Mfg. date");
                if (expiryDate == DateTime.MinValue) validationMsg.AppendLine("Invalid expiry date");
                if (weightTypeId == 0) validationMsg.AppendLine("Select Product weight");
                if (stockPrice == 0) validationMsg.AppendLine("Stock price is empty");
                if (sellingPrice == 0) validationMsg.AppendLine("Selling price is empty");
                if (weight == 0) validationMsg.AppendLine("Item weight is empty");

                if (validationMsg.Length > 0)
                {
                    validationMsgCtrl.ShowValidationBox(validationMsg.ToString());
                    return;
                }
                int.TryParse(btnAddProducts.Tag?.ToString(), out int productId);
                if (quantity > 1) productId = 0;//for copy data comfort
                var newData = new ProductModel
                {
                    Id = productId,
                    BrandId = brandNameId,
                    CreatedBy = 1, //UserSession.Instance.UserId
                    CreatedOn = DateTime.Now,
                    ExpiryDate = expiryDate,
                    IsActive = true,
                    MfgDate = manufacturedDate,
                    ProductTypeId = productTypeId,
                    SellingPrice = sellingPrice,
                    StockPrice = stockPrice,
                    WeightTypeId = weightTypeId,
                    ItemWeight = weight
                };

                List<ProductModel> productList = new List<ProductModel>();
                QRService productQR = new QRService();
                for (int i = 0; i < quantity; i++)
                {
                    var clonedData = CloneProductModel(newData);
                    var productInfo = JsonConvert.SerializeObject(newData, Formatting.Indented);
                    var imageStatus = productQR.GenerateQRCode(clonedData.QRId.ToString(), productInfo);
                    productList.Add(clonedData);
                }

                ProductService fetchService = new ProductService();
                var allData = await fetchService.SubmitProductListAsync(productList);
                BindProductGridData(allData, currentPageIndex);
                if (productId == 0)
                {
                    validationMsgCtrl.ShowValidationBox($"New product stocks {cmbBrandName.SelectedItem}, {cmbProductType.SelectedItem}, {quantity} * {cmbWeightType.SelectedItem} added successfully");
                }
                else
                {
                    validationMsgCtrl.ShowValidationBox($"product info {cmbBrandName.SelectedItem}, {cmbProductType.SelectedItem} updated.");
                }
                btnClearProduct_Click(null, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        ProductModel CloneProductModel(ProductModel original)
        {
            return new ProductModel
            {
                BrandId = original.BrandId,
                CreatedBy = original.CreatedBy,
                CreatedOn = original.CreatedOn,
                ExpiryDate = original.ExpiryDate,
                Id = original.Id,
                IsActive = original.IsActive,
                MfgDate = original.MfgDate,
                ProductTypeId = original.ProductTypeId,
                SellingPrice = original.SellingPrice,
                StockPrice = original.StockPrice,
                WeightTypeId = original.WeightTypeId,
                ItemWeight = original.ItemWeight
            };
        }

        private void btnClearProduct_Click(object sender, RoutedEventArgs e)
        {
            cmbBrandName.SelectedIndex = 0;
            cmbProductType.SelectedIndex = 0;
            cmbWeightType.SelectedIndex = 0;
            txtWeight.Text = string.Empty;
            txtQuantity.Text = string.Empty;
            txtSellingPrice.Text = string.Empty;
            txtStockPrice.Text = string.Empty;
            txtQuantity.Text = "1";
            SetDefaultValue();
            validationMsgCtrl.CloseValidationBox_Click(null, null);
            LoadControlTypeAsync();
        }

        private void btnSelectProduct_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Tag is ProductModel selectedData)
            {
                cmbBrandName.SelectedValue = selectedData.BrandId;
                cmbProductType.SelectedValue = selectedData.ProductTypeId;
                cmbProductType.Items.Refresh();
                cmbWeightType.SelectedValue = selectedData.WeightTypeId;
                txtWeight.Text = selectedData.ItemWeight.ToString();
                txtQuantity.Text = "1";
                txtSellingPrice.Text = selectedData.SellingPrice?.ToString();
                txtStockPrice.Text = selectedData.StockPrice?.ToString();
                txtMfgDate.Text = selectedData.MfgDate.ToString();
                txtExpiryDate.Text = selectedData.ExpiryDate.ToString();
                btnAddProducts.Tag = selectedData.Id;
            }
        }

        #region Text Char Limiter

        private void txtWeight_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (txtWeight.Text.IndexOf('.') > 0 && e.Text == ".")
            {
                e.Handled = true;
                return;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]*\\.?[0-9]*$"))
            {
                e.Handled = true;
            }
        }

        private void txtStockPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (txtStockPrice.Text.IndexOf('.') > 0 && e.Text == ".")
            {
                e.Handled = true;
                return;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]*\\.?[0-9]*$"))
            {
                e.Handled = true;
            }
        }

        private void txtSellingPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (txtSellingPrice.Text.IndexOf('.') > 0 && e.Text == ".")
            {
                e.Handled = true;
                return;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]*\\.?[0-9]*$"))
            {
                e.Handled = true;
            }
        }

        private void txtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$"))
            {
                e.Handled = true;
            }
        }

        #endregion

        private void btnProductSearch_Click(object sender, RoutedEventArgs e)
        {
            int currentIndex = customerPagination.CurrentPage = 1;
            var controlName = txtProductSearch.Text.Trim().ToLower();

            GetAllProductAsync(currentIndex, null, controlName, true);
        }

        private void txtProductSearch_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            int currentIndex = customerPagination.CurrentPage = 1;
            var controlName = txtProductSearch.Text.Trim().ToLower();

            GetAllProductAsync(currentIndex, null, controlName, true);
        }

        private void UpdatePaginationInfo(int currentPage, int totalPages)
        {
            customerPagination.SetPageInfo(currentPage, totalPages);
        }

        private void customerPagination_FirstPageClicked(object sender, EventArgs e)
        {
            GetAllProductAsync(1);
        }

        private void customerPagination_PreviousPageClicked(object sender, EventArgs e)
        {
            if (customerPagination.CurrentPage > 1)
            {
                --customerPagination.CurrentPage;
            }
            int currentIndex = customerPagination.CurrentPage;
            GetAllProductAsync(currentIndex);
        }

        private void customerPagination_NextPageClicked(object sender, EventArgs e)
        {
            if (customerPagination.CurrentPage != customerPagination.TotalPages)
            {
                var currentIndex = ++customerPagination.CurrentPage;
                GetAllProductAsync(currentIndex);
            }
        }

        private void customerPagination_LastPageClicked(object sender, EventArgs e)
        {
            int totalPages = customerPagination.TotalPages;
            GetAllProductAsync(totalPages);
        }

    }
}
