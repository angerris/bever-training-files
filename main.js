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

function toggleFields(executionContext) {
  const formContext = executionContext.getFormContext();
  const formType = formContext.ui.getFormType();
  const isCreateMode = formType === 1;
  const controls = formContext.ui.controls.get();
  controls.forEach((control) => {
    control.setDisabled(!isCreateMode);
  });
}

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

async function calculateChildsQuantity(executionContext) {
  const form = executionContext.getFormContext();
  const recordId = form.data.entity.getId().replace(/[{}]/g, "");
  const fetchXML = `
<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
  <entity name="cr8c9_inventory_product">
    <attribute name="cr8c9_inventory_productid" />
    <attribute name="cr8c9_int_quantity" />
    <attribute name="cr8c9_fk_product_" />
    <attribute name="cr8c9_fk_inventory" />
    <attribute name="cr8c9_name" />
    <order attribute="cr8c9_int_quantity" descending="false" />
    <filter type="and">
      <condition attribute="cr8c9_fk_inventory" operator="eq" value="${recordId}" />
    </filter>
  </entity>
</fetch>
`;
  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_inventory_product",
    "?fetchXml=" + encodeURIComponent(fetchXML)
  );
  const itemsQuantity = result.entities.length;
  form.getAttribute("cr8c9_int_items_quantity").setValue(itemsQuantity);
}

async function inventoryTotalPrice(executionContext) {
  const form = executionContext.getFormContext();
  const priceListField = form.getAttribute("cr8c9_fk_price_list").getValue();
  const priceListId = priceListField
    ? priceListField[0].id.replace(/[{}]/g, "")
    : null;
  const fetchXml = `
  <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
    <entity name="cr8c9_inventory_product">
      <attribute name="cr8c9_name"/>
      <attribute name="cr8c9_int_quantity"/>
      <attribute name="cr8c9_fk_product_"/>
      <order attribute="cr8c9_name" descending="false"/>
      <filter type="and">
        <condition attribute="cr8c9_fk_inventory" operator="eq" value="{${form.data.entity
          .getId()
          .replace(/[{}]/g, "")}}"/>
      </filter>
      <link-entity name="cr8c9_product" from="cr8c9_productid" to="cr8c9_fk_product_" link-type="inner" alias="product">
        <link-entity name="cr8c9_price_list_item" from="cr8c9_fk_product" to="cr8c9_productid" link-type="inner" alias="price">
          <attribute name="cr8c9_mon_price"/>
          <attribute name="cr8c9_fk_price_list"/>
          <filter type="and">
            <condition attribute="cr8c9_fk_price_list" operator="eq" value="${priceListId}"/>
          </filter>
        </link-entity>
      </link-entity>
    </entity>
  </fetch>`;
  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_inventory_product",
    `?fetchXml=${encodeURIComponent(fetchXml)}`
  );
  let totalAmount = 0;
  result.entities.forEach((item) => {
    const quantity = item.cr8c9_int_quantity || 0;
    const price = item["price.cr8c9_mon_price"] || 0;
    let lineTotalPrice = price * quantity;
    if (quantity === 0) {
      lineTotalPrice = price;
    }
    totalAmount += lineTotalPrice;
  });
  form.getAttribute("cr8c9_mon_total_amount").setValue(totalAmount);
}

async function autofillCurrency(executionContext) {
  const formContext = executionContext.getFormContext();
  const priceListLookup = formContext
    .getAttribute("cr8c9_fk_price_list")
    .getValue();

  if (priceListLookup && priceListLookup.length) {
    const priceListId = priceListLookup[0].id.replace("{", "").replace("}", "");
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
function setPriceListItemName(executionContext) {
  const formContext = executionContext.getFormContext();
  const product = formContext.getAttribute("cr8c9_fk_product").getValue();
  if (product !== null && product.length > 0) {
    const productName = product[0].name;
    formContext.getAttribute("cr8c9_name").setValue(productName);
  } else {
    formContext.getAttribute("cr8c9_name").setValue(null);
  }
}
