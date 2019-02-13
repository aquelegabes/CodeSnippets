// função que verifica se houve alguma mudança em alguma tabela

$(function (e) {
    var numberOfRows = $('#table>tbody>tr').length;
    $('#table').bind("DOMSubtreeModified", function () {
        if ($("#table>tbody>tr").length !== numberOfRows) {
            numberOfRows = $("#table>tbody>tr").length;
        }
    });
});
