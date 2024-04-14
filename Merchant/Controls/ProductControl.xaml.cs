﻿using MerchantDAL.EntityModel;
using MerchantDAL.Models;
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
        private int itemsPerPage = 20;
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
            var masterDatas = await fetch.GetProductMasterAsync(0, null, null);
            var brandList = masterDatas.Where(s => s.CommonControl.ControlType == "Brand")
                .Select(t => new CommonDataModel() { Id = t.Id, ControlValue = t.ControlValue }).ToList();
            brandList.Add(new CommonDataModel { Id = 0, ControlValue = "Select Brand" });
            cmbBrandName.ItemsSource = brandList.OrderBy(s => s.Id);
            cmbBrandName.SelectedIndex = 0;

            var productCategoryList = masterDatas.Where(s => s.CommonControl.ControlType == "Product Category")
                .Select(t => new CommonDataModel() { Id = t.Id, ControlValue = t.ControlValue }).ToList();
            productCategoryList.Add(new CommonDataModel { Id = 0, ControlValue = "Select Product Type" });
            cmbProductType.ItemsSource = productCategoryList.OrderBy(s => s.Id);
            cmbProductType.SelectedIndex = 0;
        }

        private async void GetAllProductAsync(int pageIndex, DateTime? expiryDate = null, string allInfo = null, bool? isActive = null)
        {
            ProductService fetchService = new ProductService();
            var allData = await fetchService.GetProductAsync(0, 0, expiryDate, allInfo, isActive);
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

        private async void btnProductList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int.TryParse(cmbBrandName.SelectedValue?.ToString(), out int brandNameId);
                int.TryParse(cmbProductType.SelectedValue?.ToString(), out int productTypeId);
                decimal.TryParse(txtWeight.Text, out decimal weightKgs);
                int.TryParse(txtQuantity.Text, out int quantity);
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
                if (weightKgs == 0) validationMsg.AppendLine("Product weight is empty");
                if (stockPrice == 0) validationMsg.AppendLine("Stock price is empty");
                if (sellingPrice == 0) validationMsg.AppendLine("Selling price is empty");

                if (validationMsg.Length > 0)
                {
                    validationMsgCtrl.ShowValidationBox(validationMsg.ToString());
                    return;
                }
                var qrGuid = Guid.NewGuid();
                var newData = new ProductModel
                {
                    BrandId = brandNameId,
                    CreatedBy = 1, //UserSession.Instance.UserId
                    CreatedOn = DateTime.Now,
                    ExpiryDate = expiryDate,
                    IsActive = true,
                    MfgDate = manufacturedDate,
                    ProductTypeId = productTypeId,
                    QRId = qrGuid,
                    SellingPrice = sellingPrice,
                    StockPrice = stockPrice,
                    WeightKgs = weightKgs
                };
                string productInfo = JsonConvert.SerializeObject(newData, Formatting.Indented);

                QRService productQR = new QRService();
                var imageStatus = productQR.GenerateQRCode(qrGuid.ToString(), productInfo);

                List<ProductModel> productList = new List<ProductModel>();
                for (int i = 0; i < quantity; i++)
                {
                    var clonedData = CloneProductModel(newData);
                    productList.Add(clonedData);
                }

                ProductService fetchService = new ProductService();
                var allData = await fetchService.SubmitProductListAsync(productList);
                BindProductGridData(allData, currentPageIndex);
                validationMsgCtrl.ShowValidationBox("New product stocks added successfully");
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
                IsActive = original.IsActive,
                MfgDate = original.MfgDate,
                ProductTypeId = original.ProductTypeId,
                QRId = original.QRId,
                SellingPrice = original.SellingPrice,
                StockPrice = original.StockPrice,
                WeightKgs = original.WeightKgs
            };
        }

        

        private void btnClearProduct_Click(object sender, RoutedEventArgs e)
        {
            cmbBrandName.SelectedIndex = 0;
            cmbProductType.SelectedIndex = 0;
            txtWeight.Text = string.Empty;
            txtQuantity.Text = string.Empty;
            txtSellingPrice.Text = string.Empty;
            txtStockPrice.Text = string.Empty;
            SetDefaultValue();
            validationMsgCtrl.CloseValidationBox_Click(null, null);
            LoadControlTypeAsync();
        }

        private void btnSelectProduct_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image image && image.Tag is Product selectedData)
            {
                cmbBrandName.SelectedIndex = 0;
                cmbProductType.SelectedIndex = 0;
                txtWeight.Text = string.Empty;
                txtQuantity.Text = string.Empty;
                txtSellingPrice.Text = string.Empty;
                txtStockPrice.Text = string.Empty;
                //txtAddressLineOne.Text = selectedData.AddressLineOne;
                //txtAddressLineTwo.Text = selectedData.AddressLineTwo;
                //txtAltMobile.Text = selectedData.AltMobile;
                //txtEmail.Text = selectedData.EmailId;
                //txtFirstName.Text = selectedData.FirstName;
                //txtLastName.Text = selectedData.LastName;
                //txtMobile.Text = selectedData.Mobile;
                //txtCity.Text = selectedData.City;
                //txtPinCode.Text = selectedData.PinCode;
                //btnAddCustomer.Tag = selectedData.Id;
            }
        }

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

        private void txtQuantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$"))
            {
                e.Handled = true;
            }

        }
        private void UpdatePaginationInfo(int currentPage, int totalPages)
        {
            customerPagination.SetPageInfo(currentPage, totalPages);
        }

        private void customerPagination_FirstPageClicked(object sender, EventArgs e)
        {

        }

        private void customerPagination_PreviousPageClicked(object sender, EventArgs e)
        {

        }

        private void customerPagination_NextPageClicked(object sender, EventArgs e)
        {

        }

        private void customerPagination_LastPageClicked(object sender, EventArgs e)
        {

        }
    }
}
