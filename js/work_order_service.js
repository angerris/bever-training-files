let servicePointer = null;

async function filterServices(executionContext) {
  const formContext = executionContext.getFormContext();

  const fetchXml = `
    <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
      <entity name="cr8c9_product">
        <attribute name="cr8c9_productid" />
        <attribute name="cr8c9_name" />
        <order attribute="cr8c9_name" descending="false" />
        <filter type="and">
          <condition attribute="cr8c9_os_type" operator="eq" value="976090001" />
        </filter>
      </entity>
    </fetch>
  `;

  const encodedFetchXml = encodeURIComponent(fetchXml);

  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_product",
    `?fetchXml=${encodedFetchXml}`
  );

  if (servicePointer !== null) {
    formContext.getControl("cr8c9_fk_service").removePreSearch(servicePointer);
  }

  if (result.entities.length > 0) {
    const serviceIds = result.entities.map(
      (service) => service["cr8c9_productid"]
    );

    servicePointer = function () {
      addServiceFilter(formContext, serviceIds);
    };

    formContext.getControl("cr8c9_fk_service").addPreSearch(servicePointer);
  } else {
    servicePointer = function () {
      applyEmptyServiceFilter(formContext);
    };

    formContext.getControl("cr8c9_fk_service").addPreSearch(servicePointer);
  }
}

function addServiceFilter(formContext, serviceIds) {
  const filterXml = `
    <filter type="and">
      <condition attribute="cr8c9_productid" operator="in">
        ${serviceIds.map((id) => `<value>${id}</value>`).join("")}
      </condition>
    </filter>
  `;

  formContext
    .getControl("cr8c9_fk_service")
    .addCustomFilter(filterXml, "cr8c9_product");
}

function applyEmptyServiceFilter(formContext) {
  const emptyFilterXml = `
    <filter type="and">
      <condition attribute="cr8c9_productid" operator="null" />
    </filter>
  `;

  formContext
    .getControl("cr8c9_fk_service")
    .addCustomFilter(emptyFilterXml, "cr8c9_product");
}

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

  const fetchXMLWorkOrder = `
      <fetch top="1">
          <entity name="cr8c9_work_order">
              <attribute name="cr8c9_fk_price_list" />
              <filter>
                  <condition attribute="cr8c9_work_orderid" operator="eq" value="${workOrderId}" />
              </filter>
          </entity>
      </fetch>
  `;

  const workOrderResult = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_work_order",
    `?fetchXml=${encodeURIComponent(fetchXMLWorkOrder)}`
  );

  const priceListId =
    workOrderResult.entities[0]._cr8c9_fk_price_list_value.replace(/[{}]/g, "");

  const fetchXMLPriceListItem = `
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

  const priceListResult = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_price_list_item",
    `?fetchXml=${encodeURIComponent(fetchXMLPriceListItem)}`
  );

  let valueToSet;

  if (priceListResult.entities && priceListResult.entities.length > 0) {
    valueToSet = priceListResult.entities[0]["cr8c9_mon_price"];
  } else {
    const productRecord = await Xrm.WebApi.retrieveRecord(
      "cr8c9_product",
      productId,
      "?$select=cr8c9_mon_cost"
    );

    valueToSet = productRecord.cr8c9_mon_cost || 0;
  }

  formContext.getAttribute("cr8c9_mon_price_per_unit").setValue(valueToSet);
}
