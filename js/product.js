function togglePricePerUnit(executionContext) {
  const formContext = executionContext.getFormContext();
  const product = 976090000;
  const assetProduct = 976090002;

  const isProductType =
    formContext.getAttribute("cr8c9_os_type").getValue() === product ||
    formContext.getAttribute("cr8c9_os_type").getValue() === assetProduct;

  formContext.getControl("cr8c9_mon_price_per_unit").setVisible(isProductType);
}
