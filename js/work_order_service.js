//------------autofill price per unit start------------
async function autofillPricePerUnitInWorkOrderService(executionContext) {
  const formContext = executionContext.getFormContext();
  const workOrderField = formContext
    .getAttribute("cr8c9_fk_work_order")
    .getValue();
  const productField = formContext.getAttribute("cr8c9_fk_service").getValue();

  if (!workOrderField || !productField) {
    return;
  }

  const workOrderId = workOrderField[0].id.replace(/[{}]/g, "");
  const productId = productField[0].id.replace(/[{}]/g, "");

  const priceListId = await getPriceListId(workOrderId);
  const valueToSet =
    (await getPricePerUnit(priceListId, productId)) ||
    (await getProductCost(productId));

  formContext.getAttribute("cr8c9_mon_price_per_unit").setValue(valueToSet);
}

async function getPriceListId(workOrderId) {
  const fetchXML = `
    <fetch top="1">
      <entity name="cr8c9_work_order">
        <attribute name="cr8c9_fk_price_list" />
        <filter>
          <condition attribute="cr8c9_work_orderid" operator="eq" value="${workOrderId}" />
        </filter>
      </entity>
    </fetch>
  `;

  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_work_order",
    `?fetchXml=${encodeURIComponent(fetchXML)}`
  );

  return result.entities[0]?._cr8c9_fk_price_list_value.replace(/[{}]/g, "");
}

async function getPricePerUnit(priceListId, productId) {
  const fetchXML = `
    <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
      <entity name="cr8c9_price_list_item">
        <attribute name="cr8c9_mon_price" />
        <filter type="and">
          <condition attribute="cr8c9_fk_price_list" operator="eq" value="${priceListId}" />
          <condition attribute="cr8c9_fk_product" operator="eq" value="${productId}" />
        </filter>
      </entity>
    </fetch>
  `;

  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_price_list_item",
    `?fetchXml=${encodeURIComponent(fetchXML)}`
  );

  return result.entities.length ? result.entities[0]["cr8c9_mon_price"] : null;
}

async function getProductCost(productId) {
  const productRecord = await Xrm.WebApi.retrieveRecord(
    "cr8c9_product",
    productId,
    "?$select=cr8c9_mon_cost"
  );

  return productRecord.cr8c9_mon_cost || 0;
}
//------------autofill price per unit end------------

//------------disable WO service field start------------
async function disableWOServiceIfWorkOrderClosed(executionContext) {
  const formContext = executionContext.getFormContext();
  const workOrderId = formContext
    .getAttribute("cr8c9_fk_work_order")
    .getValue()?.[0]?.id;

  const closedStatus = 976090001;
  if (workOrderId && (await getWorkOrderStatus(workOrderId)) === closedStatus) {
    formContext.ui.controls.forEach((control) => control.setDisabled(true));
  }
}
async function getWorkOrderStatus(workOrderId) {
  const result = await Xrm.WebApi.retrieveRecord(
    "cr8c9_work_order",
    workOrderId.replace(/[{}]/g, ""),
    "?$select=cr8c9_os_status"
  );
  return result.cr8c9_os_status;
}
//------------disable WO service field end------------

function calculateTotalAmount(executionContext) {
  const formContext = executionContext.getFormContext();
  const pricePerHour =
    formContext.getAttribute("cr8c9_mon_price_per_unit").getValue() || 0;
  const durationInMinutes =
    formContext.getAttribute("cr8c9_int_duration").getValue() || 0;
  const durationInHours = durationInMinutes / 60;
  const totalAmount = pricePerHour * durationInHours;
  formContext.getAttribute("cr8c9_mon_total_amount").setValue(totalAmount);
}
