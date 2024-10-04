function togglePricePerUnit(executionContext) {
  const formContext = executionContext.getFormContext();
  const productType = 976090000 || 976090002;
  const isProductType =
    formContext.getAttribute("cr8c9_os_type").getValue() === productType;
  formContext.getControl("cr8c9_mon_price_per_unit").setVisible(isProductType);
}
