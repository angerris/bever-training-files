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
