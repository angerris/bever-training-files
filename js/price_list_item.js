async function autofillCurrency(executionContext) {
  const formContext = executionContext.getFormContext();
  const priceListLookup = formContext
    .getAttribute("cr8c9_fk_price_list")
    .getValue();
  if (priceListLookup) {
    const priceListId = priceListLookup[0].id.replace(/[{}]/g, "");
    const result = await Xrm.WebApi.retrieveRecord(
      "cr8c9_price_list",
      priceListId,
      "?$select=transactioncurrencyid,_transactioncurrencyid_value"
    );
    const currencyId = result["_transactioncurrencyid_value"];
    const currencyName =
      result[
        "_transactioncurrencyid_value@OData.Community.Display.V1.FormattedValue"
      ];
    formContext.getAttribute("transactioncurrencyid").setValue([
      {
        id: currencyId,
        entityType: "transactioncurrency",
        name: currencyName
      }
    ]);
  }
}

function setItemName(executionContext) {
  const formContext = executionContext.getFormContext();
  const product = formContext.getAttribute("cr8c9_fk_product").getValue();
  if (product) {
    const productName = product[0].name;
    formContext.getAttribute("cr8c9_name").setValue(productName);
  } else {
    formContext.getAttribute("cr8c9_name").setValue(null);
  }
}
