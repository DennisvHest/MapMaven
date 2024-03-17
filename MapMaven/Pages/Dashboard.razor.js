function rankHistoryTooltip(chartData) {
    const dataItem = chartData.w.config.series[chartData.seriesIndex].data[chartData.dataPointIndex];

    const container = document.createElement('div');
    container.className = 'arrow_box';
    container.style.padding = '5px';

    const header = document.createElement('div');
    header.style.margin = '-5px -5px 0 -5px';
    header.style.padding = '5px';
    header.style.backgroundColor = 'black';

    const value = document.createElement('b');
    value.textContent = dataItem.x;
    header.appendChild(value);

    container.appendChild(header);

    const content = document.createElement('div');

    const rank = document.createElement('div');
    rank.style.padding = '5px 0';

    const rankLabel = document.createElement('b');
    rankLabel.textContent = 'Rank: ';
    rank.appendChild(rankLabel);

    const rankValue = document.createTextNode(dataItem.y);
    rank.appendChild(rankValue);

    content.appendChild(rank);

    const mapList = document.createElement('ul');
    mapList.style.paddingTop = '5px';
    mapList.style.borderTop = '1px solid grey';

    const data = chartData.w.config
        .series[chartData.seriesIndex]
        .data[chartData.dataPointIndex]
        .extra;

    for (let map of data) {
        const li = document.createElement('li');
        li.style.display = 'flex';
        li.style.gap = '5px';
        li.style.alignItems = 'center';

        const coverImage = document.createElement('img');
        coverImage.src = map.coverImageUrl;
        coverImage.style.width = '16px';
        coverImage.style.height = '16px';
        li.appendChild(coverImage);

        const mapName = document.createElement('span');
        mapName.textContent = map.name;

        li.appendChild(mapName);

        mapList.appendChild(li);
    }

    content.appendChild(mapList);
    container.appendChild(content);

    return container.outerHTML;
}