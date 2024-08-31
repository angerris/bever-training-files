function disableFields(executionContext) {
  const formContext = executionContext.getFormContext();
  const formType = formContext.ui.getFormType();
  const isCreateMode = formType === 1;
  const controls = formContext.ui.controls.get();
  controls.forEach((control) => {
    control.setDisabled(!isCreateMode);
  });
}

function setProductName(executionContext) {
  const formContext = executionContext.getFormContext();
  const product = formContext.getAttribute("cr8c9_fk_product_").getValue();
  if (product !== null && product.length > 0) {
    const productName = product[0].name;
    formContext.getAttribute("cr8c9_name").setValue(productName);
  } else {
    formContext.getAttribute("cr8c9_name").setValue(null);
  }
}

function calculateTotalAmount(executionContext) {
  const formContext = executionContext.getFormContext();
  const quantity = formContext.getAttribute("cr8c9_int_quantity").getValue();
  const pricePerUnit = formContext
    .getAttribute("cr8c9_mon_price_per_unit")
    .getValue();
  let totalAmount = 0;
  if (quantity !== null && pricePerUnit !== null) {
    totalAmount = quantity * pricePerUnit;
  }
  formContext.getAttribute("cr8c9_mon_total_amount").setValue(totalAmount);
}

async function autofillPricePerUnit(executionContext) {
  const formContext = executionContext.getFormContext();
  const inventory = formContext.getAttribute("cr8c9_fk_inventory").getValue();
  const product = formContext.getAttribute("cr8c9_fk_product_").getValue();
  if (!inventory || !product) {
    return;
  }
  const inventoryId = inventory[0].id.replace("{", "").replace("}", "");
  const productId = product[0].id.replace("{", "").replace("}", "");
  const inventoryRecord = await Xrm.WebApi.retrieveRecord(
    "cr8c9_inventory",
    inventoryId,
    "?$select=_cr8c9_fk_price_list_value"
  );
  const priceListId = inventoryRecord._cr8c9_fk_price_list_value;
  const priceListItemQuery = `?$filter=_cr8c9_fk_price_list_value eq ${priceListId} and _cr8c9_fk_product_value eq ${productId}&$select=cr8c9_mon_price`;
  const priceListItems = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_price_list_item",
    priceListItemQuery
  );
  let pricePerUnit;
  if (priceListItems.entities.length) {
    pricePerUnit = priceListItems.entities[0].cr8c9_mon_price;
  } else {
    const productRecord = await Xrm.WebApi.retrieveRecord(
      "cr8c9_product",
      productId,
      "?$select=cr8c9_mon_price_per_unit"
    );
    pricePerUnit = productRecord.cr8c9_mon_price_per_unit || 0;
  }
  formContext.getAttribute("cr8c9_mon_price_per_unit").setValue(pricePerUnit);
}

async function autofillCurrency(executionContext) {
  const formContext = executionContext.getFormContext();
  const inventoryLookup = formContext
    .getAttribute("cr8c9_fk_inventory")
    .getValue();

  if (inventoryLookup) {
    const inventoryId = inventoryLookup[0].id.replace("{", "").replace("}", "");
    const fetchXml = `
        <fetch>
          <entity name="cr8c9_inventory">
            <attribute name="cr8c9_fk_price_list" />
            <filter>
              <condition attribute="cr8c9_inventoryid" operator="eq" value="${inventoryId}" />
            </filter>
            <link-entity name="cr8c9_price_list" from="cr8c9_price_listid" to="cr8c9_fk_price_list" link-type="outer">
              <attribute name="transactioncurrencyid" />
            </link-entity>
          </entity>
        </fetch>`;
    const result = await Xrm.WebApi.retrieveMultipleRecords(
      "cr8c9_inventory",
      `?fetchXml=${encodeURIComponent(fetchXml)}`
    );
    if (result.entities.length) {
      const currencyId =
        result.entities[0]["cr8c9_price_list1.transactioncurrencyid"];
      if (currencyId) {
        formContext.getAttribute("transactioncurrencyid").setValue([
          {
            id: currencyId,
            entityType: "transactioncurrency",
            name: result.entities[0][
              "cr8c9_price_list1.transactioncurrencyid@OData.Community.Display.V1.FormattedValue"
            ]
          }
        ]);
      }
    }
  }
}
