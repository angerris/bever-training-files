function togglePricePerUnit(executionContext) {
  const formContext = executionContext.getFormContext();
  const typeValue = formContext.getAttribute("cr8c9_os_type").getValue();
  const pricePerUnitControl = formContext.getControl(
    "cr8c9_mon_price_per_unit"
  );
  const productTypeValue = 976090000;
  if (typeValue === productTypeValue) {
    pricePerUnitControl.setVisible(true);
  } else {
    pricePerUnitControl.setVisible(false);
  }
}
