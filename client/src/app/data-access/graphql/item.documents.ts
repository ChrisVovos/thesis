import { gql } from 'apollo-angular';

/**
 * Every GraphQL operation the client sends, in one place.
 *
 * Keeping the documents together rather than scattering them through the gateways makes the client's
 * demand on the schema reviewable at a glance, and it is the input `npm run codegen` reads when it
 * regenerates the typed document nodes.
 *
 * The selections are written out rather than shared through named fragments, and the two item
 * selections below are therefore near-identical. That repetition is deliberate: fragment matching
 * requires `__typename` on every object, and the client turns that off because it is dead weight on a
 * response whose size the study measures. Fifteen repeated lines are a smaller price than an
 * unmeasurable payload.
 */

export const SEARCH_ITEMS = gql`
  query SearchItems($criteria: ItemSearchCriteriaInput!) {
    searchItems(criteria: $criteria) {
      totalCount
      page
      pageSize
      items {
        id
        type
        status
        difficulty
        stem
        maximumScore
        categoryId
        categoryName
        authorId
        authorName
        versionNumber
        createdAtUtc
        lastModifiedAtUtc
        tags {
          id
          name
        }
      }
    }
  }
`;

export const ITEM_BY_ID = gql`
  query ItemById($id: UUID!) {
    itemById(id: $id) {
      summary {
        id
        type
        status
        difficulty
        stem
        maximumScore
        categoryId
        categoryName
        authorId
        authorName
        versionNumber
        createdAtUtc
        lastModifiedAtUtc
        tags {
          id
          name
        }
      }
      options {
        id
        text
        isCorrect
        position
        feedback
      }
      rubricGuidance
      rubricMinimumWords
      rubricMaximumWords
      sampleAnswer
      versions {
        id
        versionNumber
        publishedAtUtc
        stemText
        difficulty
        maximumScore
        options {
          text
          isCorrect
          position
          feedback
        }
      }
    }
  }
`;

export const ITEM_VERSIONS = gql`
  query ItemVersions($itemId: UUID!) {
    itemVersions(itemId: $itemId) {
      id
      versionNumber
      publishedAtUtc
      stemText
      difficulty
      maximumScore
      options {
        text
        isCorrect
        position
        feedback
      }
    }
  }
`;

export const CREATE_ITEM = gql`
  mutation CreateItem($input: CreateItemCommandInput!) {
    createItem(input: $input)
  }
`;

export const UPDATE_ITEM = gql`
  mutation UpdateItem($input: UpdateItemCommandInput!) {
    updateItem(input: $input)
  }
`;

export const DELETE_ITEM = gql`
  mutation DeleteItem($itemId: UUID!) {
    deleteItem(itemId: $itemId)
  }
`;

export const SUBMIT_ITEM = gql`
  mutation SubmitItem($itemId: UUID!) {
    submitItemForReview(itemId: $itemId)
  }
`;

export const APPROVE_ITEM = gql`
  mutation ApproveItem($itemId: UUID!) {
    approveItem(itemId: $itemId)
  }
`;

export const RETURN_ITEM_TO_DRAFT = gql`
  mutation ReturnItemToDraft($itemId: UUID!) {
    returnItemToDraft(itemId: $itemId)
  }
`;

export const PUBLISH_ITEM = gql`
  mutation PublishItem($itemId: UUID!) {
    publishItem(itemId: $itemId)
  }
`;

export const RETIRE_ITEM = gql`
  mutation RetireItem($itemId: UUID!) {
    retireItem(itemId: $itemId)
  }
`;

export const CATEGORIES = gql`
  query Categories {
    categories {
      id
      name
      description
      parentCategoryId
      isActive
      itemCount
    }
  }
`;

export const TAGS = gql`
  query Tags {
    tags {
      id
      name
      itemCount
    }
  }
`;

export const CREATE_TAG = gql`
  mutation CreateTag($name: String!) {
    createTag(name: $name)
  }
`;
